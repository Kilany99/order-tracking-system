using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using OrderService.API.Clients;
using OrderService.API.Hubs;
using OrderService.API.Models;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.Serialization;
using OrderService.Infrastructure.Services;
using RestSharp.Extensions;
using System.Threading.Channels;
using static Confluent.Kafka.ConfigPropertyNames;

namespace OrderService.API.Consumers;
public class KafkaConsumerService : BackgroundService
{
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly CancellationTokenSource _stoppingCts;
    private readonly Channel<OrderCreatedEvent> _orderChannel;
    private readonly Channel<DriverLocationEvent> _locationChannel;
    private readonly IProducer<string,OrderAssignmentFailedEvent> _failProducer;
    public KafkaConsumerService(
        ILogger<KafkaConsumerService> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _stoppingCts = new CancellationTokenSource();
        _orderChannel = Channel.CreateUnbounded<OrderCreatedEvent>();
        _locationChannel = Channel.CreateUnbounded<DriverLocationEvent>();
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = configuration["kafka:BootstrapServers"],
            EnableIdempotence = true,
            MessageSendMaxRetries = 3,
            Acks = Acks.All
        };

        _failProducer = new ProducerBuilder<string, OrderAssignmentFailedEvent>(producerConfig)
            .SetValueSerializer(new JsonSerializer<OrderAssignmentFailedEvent>())
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Kafka consumers...");

        try
        {
            // Create tasks for both consumers and processors
            var tasks = new List<Task>
            {
                Task.Run(() => ConsumeOrderMessages(stoppingToken), stoppingToken),
                Task.Run(() => ConsumeLocationMessages(stoppingToken), stoppingToken),
                Task.Run(() => ProcessOrderMessages(stoppingToken), stoppingToken),
                Task.Run(() => ProcessLocationMessages(stoppingToken), stoppingToken)
            };

            // Wait for any task to complete (or fail)
            await Task.WhenAny(tasks);

            // If we get here, one of the tasks has completed/failed
            var faultedTask = tasks.FirstOrDefault(t => t.IsFaulted);
            if (faultedTask != null && faultedTask.Exception != null)
            {
                _logger.LogError(faultedTask.Exception, "A consumer task has failed");
                throw faultedTask.Exception;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in Kafka consumer service");
            throw;
        }
    }

    private async Task ConsumeOrderMessages(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = $"{_configuration["Kafka:GroupId"]}-orders",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        try
        {
            using var consumer = new ConsumerBuilder<string, OrderCreatedEvent>(config)
                .SetValueDeserializer(new JsonDeserializer<OrderCreatedEvent>())
                .Build();

            var topic = _configuration["Kafka:OrderCreated"];
            consumer.Subscribe(topic);
            _logger.LogInformation("Order consumer started for topic: {Topic}", topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(100));
                    if (result?.Message?.Value == null) continue;

                    _logger.LogInformation("Received order message for OrderId: {OrderId}",
                        result.Message.Value.OrderId);

                    await _orderChannel.Writer.WriteAsync(result.Message.Value, stoppingToken);
                    consumer.Commit(result);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error consuming order message");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Order consumer stopping");
        }
        finally
        {
            _logger.LogInformation("Order consumer stopped");
            _orderChannel.Writer.Complete();
        }
    }

    private async Task ConsumeLocationMessages(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = $"{_configuration["Kafka:GroupId"]}-locations",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        try
        {
            using var consumer = new ConsumerBuilder<string, DriverLocationEvent>(config)
                .SetValueDeserializer(new JsonDeserializer<DriverLocationEvent>())
                .Build();

            var topic = _configuration["Kafka:Topic"];
            consumer.Subscribe(topic);
            _logger.LogInformation("Location consumer started for topic: {Topic}", topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(100));
                    if (result?.Message?.Value == null) continue;
                    var location = result.Message.Value;

                    // Validate the location event
                    if (location.DriverId == Guid.Empty)
                    {
                        _logger.LogWarning("Received location update with empty DriverId");
                        continue;
                    }
                    _logger.LogInformation("Received location update for DriverId: {DriverId} at {Latitude}, {Longitude}",
                        location.DriverId,location.Latitude,location.Longitude);

                    await _locationChannel.Writer.WriteAsync(result.Message.Value, stoppingToken);
                    consumer.Commit(result);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error consuming location message");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Location consumer stopping");
        }
        finally
        {
            _logger.LogInformation("Location consumer stopped");
            _locationChannel.Writer.Complete();
        }
    }

    private async Task ProcessOrderMessages(CancellationToken stoppingToken)
    {
        var retryConfig = _configuration.GetSection("RetryPolicy");
        var maxRetryAttempts = retryConfig.GetValue<int>("MaxRetryAttempts");
        var initialDelayMs = retryConfig.GetValue<int>("InitialDelayMs");
        var maxDelayMs = retryConfig.GetValue<int>("MaxDelayMs");

        using var scope = _scopeFactory.CreateScope();
        var driverClient = scope.ServiceProvider.GetRequiredService<IDriverClient>();
        var producer = scope.ServiceProvider.GetRequiredService<IProducer<string, DriverAssignedEvent>>();
        Exception? lastError = null;
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

        await foreach (var orderEvent in _orderChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Processing order {OrderId}", orderEvent.OrderId);

                // Implementing a retry mechanisim with exponential backoff until assign a driver to the order
                int retryCount = 0;
                TimeSpan delay = TimeSpan.FromMilliseconds(initialDelayMs);
                Guid? driverId = null;
                while (!stoppingToken.IsCancellationRequested && driverId == null)
                {
                    try
                    {
                        driverId = await driverClient.AssignDriverToOrderAsync(
                            orderEvent.DeliveryLatitude,
                            orderEvent.DeliveryLongitude,
                            orderEvent.OrderId);
                    }
                    catch (Exception ex) when (retryCount < maxRetryAttempts)
                    {
                        lastError = ex;
                        retryCount++;
                        _logger.LogWarning("Driver assignment failed for order {OrderId}. Attempt {RetryCount}/{MaxRetries}. Retrying in {Delay}ms",
                            orderEvent.OrderId, retryCount, maxRetryAttempts, delay.TotalMilliseconds);

                        await Task.Delay(delay, stoppingToken);
                        delay = TimeSpan.FromMilliseconds(
                            Math.Min(delay.TotalMilliseconds * 2, maxDelayMs));
                    }
                }
                if (driverId == null) //If we reach here that means max retries has been exceeded and driver not assigned 
                {
                    _logger.LogError("Permanent failure assigning driver to order {OrderId} after {MaxRetries} attempts",
                        orderEvent.OrderId, maxRetryAttempts);
                    await OnAssignmentFailure(orderEvent, lastError, orderRepository, stoppingToken);
                     continue;
                }
                var order = await orderRepository.GetByIdAsync(orderEvent.OrderId);
                if (order == null)
                {
                    _logger.LogError("Order not found with ID {OrderId}", orderEvent.OrderId);
                    continue;
                }

                order.DriverId = driverId;
                order.MarkAsPreparing();
                await orderRepository.SaveChangesAsync();
                _logger.LogInformation("Order {OrderId} info has been updated with the driverId {driverId} ", orderEvent.OrderId, driverId);

                await producer.ProduceAsync("drivers-assigned", new Message<string, DriverAssignedEvent>
                {
                    Key = orderEvent.OrderId.ToString(),
                    Value = new DriverAssignedEvent(orderEvent.OrderId, driverId.Value, DateTime.UtcNow)
                }, stoppingToken);

                _logger.LogInformation("Assigned driver {DriverId} to order {OrderId}",
                    driverId, orderEvent.OrderId);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Processing cancelled");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error processing order {OrderId}", orderEvent.OrderId);
                await OnAssignmentFailure(orderEvent, ex,orderRepository,stoppingToken);
              
            }
        }
    }

    private async Task ProcessLocationMessages(CancellationToken stoppingToken)
    {
        await foreach (var locationEvent in _locationChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<TrackingHub>>();
                var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

                _logger.LogInformation("Processing location update for driver {DriverId}",
                    locationEvent.DriverId);

                var orders = await orderRepository.GetOrdersByDriver(locationEvent.DriverId);
                if (!orders.Any())
                {
                    _logger.LogInformation(
                        "No active orders found for driver {DriverId}",
                        locationEvent.DriverId);
                    return;
                }
                
                foreach (var order in orders)
                {
                   
                    await hubContext.Clients.Group(order.Id.ToString())
                        .SendAsync("DriverLocationUpdate", new
                        {
                            orderId = order.Id,
                            driverId = locationEvent.DriverId,
                            lat = locationEvent.Latitude,
                            lng = locationEvent.Longitude,
                            timestamp = locationEvent.TimeSpan
                        }, stoppingToken);

                    _logger.LogInformation(
                        "Sent location update for driver {DriverId} to order {OrderId}",
                        locationEvent.DriverId, order.Id);
                }
                // Update driver's location in cache
                var cacheService = scope.ServiceProvider.GetRequiredService<RedisCacheService>();
                _logger.LogInformation(
                        "Caching location update for driver {DriverId} ...",
                        locationEvent.DriverId);
                await cacheService.CacheDriverLocationAsync(
                    locationEvent.DriverId,
                    locationEvent.Latitude,
                    locationEvent.Longitude
                    );
                _logger.LogInformation(
                      "Cached location update successfully for {DriverId}",
                      locationEvent.DriverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing location for driver {DriverId}",
                    locationEvent.DriverId);
            }
        }
    }
    private async Task OnAssignmentFailure(OrderCreatedEvent orderEvent,Exception? lastError,IOrderRepository orderRepository, CancellationToken stoppingToken)
    {

        _logger.LogInformation("Producing OrderAssignFaild event...");
        // Publish failure event
        await _failProducer.ProduceAsync("order-assignment-failed", new Message<string, OrderAssignmentFailedEvent>
        {
            Key = orderEvent.OrderId.ToString(),
            Value = new OrderAssignmentFailedEvent(
                OrderId: orderEvent.OrderId,
                Reason: lastError?.Message ?? "No available drivers",
                FailedAt: DateTime.UtcNow
            )
        }, stoppingToken);
        _logger.LogInformation("Canceling order with Id {orderId}...", orderEvent.OrderId);
        // Cancel the Order
        var orderToBeCanclled = await orderRepository.GetByIdAsync(orderEvent.OrderId);
        orderToBeCanclled.MarkAsCancelled();
        await orderRepository.SaveChangesAsync();
        _logger.LogInformation("order with Id {orderId} has been canclled.", orderEvent.OrderId);


    }
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Kafka consumers...");
        _stoppingCts.Cancel();
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _stoppingCts.Dispose();
        base.Dispose();
    }
}