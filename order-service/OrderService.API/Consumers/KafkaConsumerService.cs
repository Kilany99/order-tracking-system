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

namespace OrderService.API.Consumers;
public class KafkaConsumerService : BackgroundService
{
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly CancellationTokenSource _stoppingCts;
    private readonly Channel<OrderCreatedEvent> _orderChannel;
    private readonly Channel<DriverLocationEvent> _locationChannel;

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
        await foreach (var orderEvent in _orderChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var driverClient = scope.ServiceProvider.GetRequiredService<IDriverClient>();
                var producer = scope.ServiceProvider.GetRequiredService<IProducer<string, DriverAssignedEvent>>();
                var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                _logger.LogInformation("Processing order {OrderId}", orderEvent.OrderId);
               

                var driverId = await driverClient.AssignDriverToOrderAsync(
                    orderEvent.DeliveryLatitude,
                    orderEvent.DeliveryLongitude,
                    orderEvent.OrderId);
                // Assign driverId to the order
                var order = await orderRepository.GetByIdAsync(orderEvent.OrderId);
                if(order==null)
                { 
                    _logger.LogError("Order not found with the sent OrderId {orderId}",
                            orderEvent.OrderId);
                    return;
                }


                order.DriverId = driverId;
                order.MarkAsPreparing();
                await orderRepository.SaveChangesAsync();
                _logger.LogInformation("Order {OrderId} info has been updated with the driverId {driverId} ", orderEvent.OrderId, driverId);

                await producer.ProduceAsync("drivers-assigned", new Message<string, DriverAssignedEvent>
                {
                    Key = orderEvent.OrderId.ToString(),
                    Value = new DriverAssignedEvent(orderEvent.OrderId, driverId, DateTime.UtcNow)
                }, stoppingToken);

                _logger.LogInformation("Assigned driver {DriverId} to order {OrderId}",
                    driverId, orderEvent.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order {OrderId}", orderEvent.OrderId);
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