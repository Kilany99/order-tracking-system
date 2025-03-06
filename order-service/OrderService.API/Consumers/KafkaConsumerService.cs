using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;

using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Models;
using OrderService.Infrastructure.Channels;
using OrderService.Infrastructure.Clients;
using OrderService.Infrastructure.Hubs;
using OrderService.Infrastructure.Metrics;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.Serialization;
using OrderService.Infrastructure.Services;
using System.Threading.Channels;

namespace OrderService.API.Consumers;
public class KafkaConsumerService : BackgroundService
{
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly CancellationTokenSource _stoppingCts;
    private readonly IOrderProcessingChannel<OrderCreatedEvent> _orderChannel;
    private readonly IOrderProcessingChannel<DriverLocationEvent> _locationChannel;
    private readonly IOrderProcessingChannel<OrderPickedUpEvent> _pickupChannel;
    private readonly IOrderProcessingChannel<OrderDeliveredEvent> _deliveredchannel;
    private readonly IProducer<string,OrderAssignmentFailedEvent> _failProducer;

    private readonly OrderMetrics _metrics;

    public KafkaConsumerService(
        ILogger<KafkaConsumerService> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        IOrderProcessingChannel<OrderCreatedEvent> orderCreatedChannel,
        IOrderProcessingChannel<OrderPickedUpEvent> pickupChannel,
        IOrderProcessingChannel<DriverLocationEvent> locationChannel,
        IOrderProcessingChannel<OrderDeliveredEvent> deliveredchannel,
        OrderMetrics metrics)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _stoppingCts = new CancellationTokenSource();
        _orderChannel = orderCreatedChannel;
        _pickupChannel = pickupChannel;
        _locationChannel = locationChannel;
        _deliveredchannel = deliveredchannel;
        _metrics = metrics;
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
                Task.Run(() => ProcessLocationMessages(stoppingToken), stoppingToken),
                Task.Run(() => ConsumeOrderPickupMessages(stoppingToken), stoppingToken),
                Task.Run(() => ProcessOrderPickupMessages(stoppingToken), stoppingToken),
                Task.Run(() => ConsumeOrderDeliveredMessages(stoppingToken), stoppingToken),
                Task.Run(() => ProcessOrderDeliveredMessages(stoppingToken), stoppingToken),

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
        var orderUpdateService = scope.ServiceProvider.GetRequiredService<IOrderUpdateService>();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        Exception? lastError = null;

        await foreach (var orderEvent in _orderChannel.Reader.ReadAllAsync(stoppingToken))
        {
            using var timer = _metrics.BeginMessageProcessing("order");
            try
            {
                _logger.LogInformation("Processing order {OrderId}", orderEvent.OrderId);

                var order = await orderRepository.GetByIdAsync(orderEvent.OrderId);
                if (order == null)
                {
                    _logger.LogError("Order not found with ID {OrderId}", orderEvent.OrderId);
                    continue;
                }
                // Check if order already has a driver
                if (order.DriverId.HasValue)
                {
                    _logger.LogInformation(
                        "Order {OrderId} already has driver {DriverId} assigned",
                        order.Id,
                        order.DriverId);

                    // Make sure status is updated
                    if (order.Status == OrderStatus.Created)
                    {
                        order.MarkAsPreparing();
                        await orderRepository.SaveChangesAsync();

                        await orderUpdateService.SendOrderStatusUpdate(
                            order.Id,
                            OrderStatus.Preparing,
                            order.DriverId);
                    }
                    continue;
                }

                // Send initial status update only if this is the first attempt
                if (order.AssignmentRetryCount == 0)
                {
                    await orderUpdateService.SendOrderStatusUpdate(
                        orderEvent.OrderId,
                        OrderStatus.Created);
                }

                // Initialize retry parameters
                int retryCount = order.AssignmentRetryCount;
                TimeSpan delay = TimeSpan.FromMilliseconds(
                    Math.Min(initialDelayMs * Math.Pow(2, retryCount), maxDelayMs));
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

                        // Update order with retry information
                        order.AssignmentRetryCount = retryCount;
                        order.LastAssignmentAttempt = DateTime.UtcNow;
                        order.NextAssignmentAttempt = DateTime.UtcNow.Add(delay);
                        await orderRepository.SaveChangesAsync();

                        _logger.LogWarning(
                            "Driver assignment failed for order {OrderId}. Attempt {RetryCount}/{MaxRetries}. Retrying in {Delay}ms",
                            orderEvent.OrderId, retryCount, maxRetryAttempts, delay.TotalMilliseconds);

                        await Task.Delay(delay, stoppingToken);
                        delay = TimeSpan.FromMilliseconds(
                            Math.Min(delay.TotalMilliseconds * 2, maxDelayMs));
                    }
                }

                if (driverId == null) // Max retries exceeded
                {
                    _logger.LogError(
                        "Permanent failure assigning driver to order {OrderId} after {MaxRetries} attempts",
                        orderEvent.OrderId, maxRetryAttempts);
                    await OnAssignmentFailure(orderEvent, lastError, orderRepository, orderUpdateService, stoppingToken);
                    continue;
                }
                _logger.LogInformation("Updating order {OrderId} with the found driverId {driverId}", orderEvent.OrderId,driverId);

                // Assignment successful - update order
                order.DriverId = driverId;
                order.MarkAsPreparing();
                order.AssignmentRetryCount = retryCount;
                order.LastAssignmentAttempt = DateTime.UtcNow;
                order.NextAssignmentAttempt = null; // Clear next attempt as assignment succeeded
                await orderRepository.SaveChangesAsync();
                _logger.LogInformation("Sending preparing status to signalR with driver ID {diverId}", driverId);

                // Send preparing status with driver ID
                await orderUpdateService.SendOrderStatusUpdate(
                    orderEvent.OrderId,
                    OrderStatus.Preparing,
                    driverId);

                _logger.LogInformation(
                    "Order {OrderId} info has been updated with the driverId {driverId}",
                    orderEvent.OrderId, driverId);

                // Publish driver assigned event
                await producer.ProduceAsync(
                    "drivers-assigned",
                    new Message<string, DriverAssignedEvent>
                    {
                        Key = orderEvent.OrderId.ToString(),
                        Value = new DriverAssignedEvent(orderEvent.OrderId, driverId.Value, DateTime.UtcNow)
                    },
                    stoppingToken);
                _metrics.RecordMessageProcessed("order");
                if (driverId != null)
                {
                    _metrics.RecordDriverAssignmentAttempt(true);
                }
                else
                {
                    _metrics.RecordDriverAssignmentAttempt(false);
                }
                _logger.LogInformation(
                    "Assigned driver {DriverId} to order {OrderId}",
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
                _metrics.RecordMessageError("order");
                await OnAssignmentFailure(orderEvent, lastError, orderRepository, orderUpdateService, stoppingToken);
            }
        }
    }

    
    private async Task ProcessLocationMessages(CancellationToken stoppingToken)
    {
        await foreach (var locationEvent in _locationChannel.Reader.ReadAllAsync(stoppingToken))
        {
            using var timer = _metrics.BeginMessageProcessing("location");
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

                _metrics.RecordMessageProcessed("location");

                _logger.LogInformation(
                      "Cached location update successfully for {DriverId}",
                      locationEvent.DriverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing location for driver {DriverId}",
                    locationEvent.DriverId);
                _metrics.RecordMessageError("location");

            }
        }
    }
    private async Task OnAssignmentFailure(OrderCreatedEvent orderEvent,Exception? lastError,
        IOrderRepository orderRepository, IOrderUpdateService orderUpdateService, CancellationToken stoppingToken)
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
        await orderUpdateService.SendOrderStatusUpdate(
                     orderEvent.OrderId,
                     OrderStatus.Cancelled);
        _logger.LogInformation("Canceling order with Id {orderId}...", orderEvent.OrderId);
        // Cancel the Order
        var orderToBeCanclled = await orderRepository.GetByIdAsync(orderEvent.OrderId);
        orderToBeCanclled.MarkAsCancelled();
        await orderRepository.SaveChangesAsync();
        _logger.LogInformation("order with Id {orderId} has been canclled.", orderEvent.OrderId);


    }


    private async Task ConsumeOrderPickupMessages(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = $"{_configuration["Kafka:GroupId"]}-pickups",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        try
        {
            using var consumer = new ConsumerBuilder<string, OrderPickedUpEvent>(config)
                .SetValueDeserializer(new JsonDeserializer<OrderPickedUpEvent>())
                .Build();

            var topic = _configuration["Kafka:OrderPickedUp"];
            consumer.Subscribe(topic);
            _logger.LogInformation("Order pickup consumer started for topic: {Topic}", topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(100));
                    if (result?.Message?.Value == null) continue;

                    await _pickupChannel.Writer.WriteAsync(result.Message.Value, stoppingToken);
                    consumer.Commit(result);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error consuming order pickup message");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Order pickup consumer stopping");
        }
    }

    private async Task ProcessOrderPickupMessages(CancellationToken stoppingToken)
    {
        await foreach (var pickupEvent in _pickupChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                var orderUpdateService = scope.ServiceProvider.GetRequiredService<IOrderUpdateService>();

                var order = await orderRepository.GetByIdAsync(pickupEvent.OrderId);
                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for pickup event", pickupEvent.OrderId);
                    continue;
                }

                order.MarkAsOutForDelivery();
                await orderRepository.SaveChangesAsync();

                await orderUpdateService.SendOrderStatusUpdate(
                    pickupEvent.OrderId,
                    OrderStatus.Delivered,
                    pickupEvent.DriverId);

                _logger.LogInformation(
                    "Order {OrderId} marked as out for delivery by driver {DriverId}",
                    pickupEvent.OrderId,
                    pickupEvent.DriverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pickup event for order {OrderId}", pickupEvent.OrderId);
            }
        }
    }


    private async Task ConsumeOrderDeliveredMessages(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = $"{_configuration["Kafka:GroupId"]}-deliveries",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        try
        {
            using var consumer = new ConsumerBuilder<string, OrderDeliveredEvent>(config)
                .SetValueDeserializer(new JsonDeserializer<OrderDeliveredEvent>())
                .Build();

            var topic = _configuration["Kafka:OrderDelivered"];
            consumer.Subscribe(topic);
            _logger.LogInformation("Order delivered consumer started for topic: {Topic}", topic);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(100));
                    if (result?.Message?.Value == null) continue;

                    await _deliveredchannel.Writer.WriteAsync(result.Message.Value, stoppingToken);
                    consumer.Commit(result);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error consuming order delivered message");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Order delivered consumer stopping");
        }
    }

    private async Task ProcessOrderDeliveredMessages(CancellationToken stoppingToken)
    {
        await foreach (var deliveredEvent in _deliveredchannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                var orderUpdateService = scope.ServiceProvider.GetRequiredService<IOrderUpdateService>();

                var order = await orderRepository.GetByIdAsync(deliveredEvent.OrderId);
                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found for delivered event", deliveredEvent.OrderId);
                    continue;
                }

                order.MarkAsDelivered();
                await orderRepository.SaveChangesAsync();

                await orderUpdateService.SendOrderStatusUpdate(
                    deliveredEvent.OrderId,
                    OrderStatus.Delivered,
                    deliveredEvent.DriverId);

                _logger.LogInformation(
                    "Order {OrderId} delivered by driver {DriverId}",
                    deliveredEvent.OrderId,
                    deliveredEvent.DriverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing delivered event for order {OrderId}", deliveredEvent.OrderId);
            }
        }
    }

    private async Task EnsureOrderStatusConsistency(
    Order order,
    IOrderRepository repository,
    IOrderUpdateService updateService)
    {
        if (order.DriverId.HasValue && order.Status == OrderStatus.Created)
        {
            _logger.LogWarning(
                "Inconsistent state detected: Order {OrderId} has driver but status is Created",
                order.Id);

            order.MarkAsPreparing();
            await repository.SaveChangesAsync();

            await updateService.SendOrderStatusUpdate(
                order.Id,
                OrderStatus.Preparing,
                order.DriverId);
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