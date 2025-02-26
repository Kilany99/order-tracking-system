using Confluent.Kafka;
using NotificationService.Domain.Events;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Serialization;
namespace NotificationService.API.Consumers;

public class NotificationConsumerService : BackgroundService
{
    private readonly ILogger<NotificationConsumerService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly CancellationTokenSource _stoppingCts;
    private readonly INotificationChannel<OrderNotificationEvent> _orderCreatedChannel;
    private readonly INotificationChannel<OrderNotificationEvent> _driverAssignedChannel;
    private readonly INotificationChannel<OrderNotificationEvent> _orderPickedUpChannel;
    private readonly INotificationChannel<OrderNotificationEvent> _orderDeliveredChannel;
    private readonly OrderEventDeserializer _deserializer;


    public NotificationConsumerService(
        ILogger<NotificationConsumerService> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        OrderEventDeserializer deserializer,
        INotificationChannel<OrderNotificationEvent> orderCreatedChannel,
        INotificationChannel<OrderNotificationEvent> driverAssignedChannel,
        INotificationChannel<OrderNotificationEvent> orderPickedUpChannel,
        INotificationChannel<OrderNotificationEvent> orderDeliveredChannel)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _deserializer = deserializer;
        _stoppingCts = new CancellationTokenSource();
        _orderCreatedChannel = orderCreatedChannel;
        _driverAssignedChannel = driverAssignedChannel;
        _orderPickedUpChannel = orderPickedUpChannel;
        _orderDeliveredChannel = orderDeliveredChannel;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Kafka consumers...");

        try
        {
            var tasks = new List<Task>
            {
                Task.Run(() => ConsumeOrderCreatedMessages(stoppingToken), stoppingToken),
                Task.Run(() => ConsumeDriverAssignedMessages(stoppingToken), stoppingToken),
                Task.Run(() => ConsumeOrderPickupMessages(stoppingToken), stoppingToken),
                Task.Run(() => ConsumeOrderDeliveredMessages(stoppingToken), stoppingToken),
                Task.Run(() => ProcessOrderCreatedMessages(stoppingToken), stoppingToken),
                Task.Run(() => ProcessDriverAssignedMessages(stoppingToken), stoppingToken),
                Task.Run(() => ProcessOrderPickupMessages(stoppingToken), stoppingToken),
                Task.Run(() => ProcessOrderDeliveredMessages(stoppingToken), stoppingToken)
            };

            await Task.WhenAny(tasks);

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

    private async Task ConsumeOrderCreatedMessages(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = $"{_configuration["Kafka:GroupId"]}-order-created",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        try
        {
            using var consumer = new ConsumerBuilder<string, OrderNotificationEvent>(config)
                .SetValueDeserializer(_deserializer)
                .Build();

            consumer.Subscribe("OrderCreated");
            _logger.LogInformation("Order created consumer started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(100));
                    if (result?.Message?.Value == null) continue;

                    await _orderCreatedChannel.Writer.WriteAsync(result.Message.Value, stoppingToken);
                    consumer.Commit(result);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error consuming order created message");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Order created consumer stopping");
        }
    }

    private async Task ConsumeDriverAssignedMessages(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = $"{_configuration["Kafka:GroupId"]}-driver-assigned",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        try
        {
            using var consumer = new ConsumerBuilder<string, OrderNotificationEvent>(config)
                .SetValueDeserializer(_deserializer)
                .Build();

            consumer.Subscribe("drivers-assigned");
            _logger.LogInformation("Driver assigned consumer started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(100));
                    if (result?.Message?.Value == null) continue;

                    await _driverAssignedChannel.Writer.WriteAsync(result.Message.Value, stoppingToken);
                    consumer.Commit(result);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error consuming driver assigned message");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Driver assigned consumer stopping");
        }
    }

    private async Task ConsumeOrderPickupMessages(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = $"{_configuration["Kafka:GroupId"]}-order-pickup",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        try
        {
            using var consumer = new ConsumerBuilder<string, OrderNotificationEvent>(config)
                .SetValueDeserializer(_deserializer)
                .Build();

            consumer.Subscribe("order-picked-up");
            _logger.LogInformation("Order pickup consumer started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(100));
                    if (result?.Message?.Value == null) continue;

                    await _orderPickedUpChannel.Writer.WriteAsync(result.Message.Value, stoppingToken);
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

    private async Task ConsumeOrderDeliveredMessages(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"],
            GroupId = $"{_configuration["Kafka:GroupId"]}-order-delivered",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        try
        {
            using var consumer = new ConsumerBuilder<string, OrderNotificationEvent>(config)
                .SetValueDeserializer(_deserializer)
                .Build();

            consumer.Subscribe("order-delivered");
            _logger.LogInformation("Order delivered consumer started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(100));
                    if (result?.Message?.Value == null) continue;

                    await _orderDeliveredChannel.Writer.WriteAsync(result.Message.Value, stoppingToken);
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

    private async Task ProcessOrderCreatedMessages(CancellationToken stoppingToken)
    {
        await foreach (var notification in _orderCreatedChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var notificationHandler = scope.ServiceProvider.GetRequiredService<INotificationHandler>();

                notification.Status = OrderStatus.Created;
                await notificationHandler.HandleOrderNotificationAsync(notification);

                _logger.LogInformation(
                    "Processed order created notification for order {OrderId}",
                    notification.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing order created notification for order {OrderId}",
                    notification.OrderId);
            }
        }
    }

    private async Task ProcessDriverAssignedMessages(CancellationToken stoppingToken)
    {
        await foreach (var notification in _driverAssignedChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var notificationHandler = scope.ServiceProvider.GetRequiredService<INotificationHandler>();

                notification.Status = OrderStatus.Assigned;
                await notificationHandler.HandleOrderNotificationAsync(notification);

                _logger.LogInformation(
                    "Processed driver assigned notification for order {OrderId}, driver {DriverId}",
                    notification.OrderId,
                    notification.DriverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing driver assigned notification for order {OrderId}",
                    notification.OrderId);
            }
        }
    }

    private async Task ProcessOrderPickupMessages(CancellationToken stoppingToken)
    {
        await foreach (var notification in _orderPickedUpChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var notificationHandler = scope.ServiceProvider.GetRequiredService<INotificationHandler>();

                notification.Status = OrderStatus.OutForDelivery;
                await notificationHandler.HandleOrderNotificationAsync(notification);

                _logger.LogInformation(
                    "Processed order pickup notification for order {OrderId}, driver {DriverId}",
                    notification.OrderId,
                    notification.DriverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing order pickup notification for order {OrderId}",
                    notification.OrderId);
            }
        }
    }

    private async Task ProcessOrderDeliveredMessages(CancellationToken stoppingToken)
    {
        await foreach (var notification in _orderDeliveredChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var notificationHandler = scope.ServiceProvider.GetRequiredService<INotificationHandler>();

                notification.Status = OrderStatus.Delivered;
                await notificationHandler.HandleOrderNotificationAsync(notification);

                _logger.LogInformation(
                    "Processed order delivered notification for order {OrderId}, driver {DriverId}",
                    notification.OrderId,
                    notification.DriverId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing order delivered notification for order {OrderId}",
                    notification.OrderId);
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