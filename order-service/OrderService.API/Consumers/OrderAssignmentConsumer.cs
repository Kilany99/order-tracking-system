using Confluent.Kafka;
using OrderService.API.Clients;
using OrderService.API.Models;
using OrderService.Infrastructure.Serialization;

namespace OrderService.API.Consumers;
public class OrderAssignmentConsumer : BackgroundService
{
    private readonly ILogger<OrderAssignmentConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _topic;
    private readonly string _bootstrapServers;
    private IConsumer<string, OrderCreatedEvent> _consumer;
    private readonly CancellationTokenSource _cancellationTokenSource;

    public OrderAssignmentConsumer(
        ILogger<OrderAssignmentConsumer> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration config)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _topic = config["Kafka:OrderCreated"] ?? "orders-created";
        _bootstrapServers = config["Kafka:BootstrapServers"];
        _cancellationTokenSource = new CancellationTokenSource();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield(); // Ensure we don't block startup

        try
        {
            await StartConsumerLoop(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in consumer loop");
        }
    }

    private async Task StartConsumerLoop(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = "order-assignment-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            MaxPollIntervalMs = 300000,
            SessionTimeoutMs = 45000,
            HeartbeatIntervalMs = 15000
        };

        using (_consumer = new ConsumerBuilder<string, OrderCreatedEvent>(consumerConfig)
            .SetValueDeserializer(new JsonDeserializer<OrderCreatedEvent>())
            .Build())
        {
            await Task.Run(async () =>
            {
                try
                {
                    _consumer.Subscribe(_topic);
                    _logger.LogInformation("Consumer started for topic: {Topic}", _topic);

                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = _consumer.Consume(TimeSpan.FromMilliseconds(100));
                            if (consumeResult == null) continue;

                            // Process the message in a separate task
                            await ProcessMessageAsync(consumeResult, stoppingToken);
                        }
                        catch (ConsumeException e)
                        {
                            _logger.LogError(e, "Consume error");
                            await Task.Delay(1000, stoppingToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Consumer stopping");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in consumer loop");
                }
                finally
                {
                    try
                    {
                        _consumer.Close();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error closing consumer");
                    }
                }
            }, stoppingToken);
        }
    }

    private async Task ProcessMessageAsync(ConsumeResult<string, OrderCreatedEvent> consumeResult, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var orderEvent = consumeResult.Message.Value;

            _logger.LogInformation(
                "Processing order {OrderId} from partition {Partition}",
                orderEvent.OrderId,
                consumeResult.Partition.Value);

            await ProcessOrderAssignment(orderEvent, stoppingToken);

            // Commit offset after successful processing
            _consumer.Commit(consumeResult);

            _logger.LogInformation(
                "Successfully processed order {OrderId}",
                orderEvent.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing message for order {OrderId}",
                consumeResult.Message.Value.OrderId);
            throw;
        }
    }

    private async Task ProcessOrderAssignment(OrderCreatedEvent orderEvent, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var driverClient = services.GetRequiredService<IDriverClient>();
            var producer = services.GetRequiredService<IProducer<string, DriverAssignedEvent>>();

            var driverId = await driverClient.FindAvailableDriverAsync(
                orderEvent.DeliveryLatitude,
                orderEvent.DeliveryLongitude);

            await producer.ProduceAsync("drivers-assigned", new Message<string, DriverAssignedEvent>
            {
                Key = orderEvent.OrderId.ToString(),
                Value = new DriverAssignedEvent(
                    orderEvent.OrderId,
                    driverId,
                    DateTime.UtcNow)
            }, ct);

            _logger.LogInformation(
                "Assigned driver {DriverId} to order {OrderId}",
                driverId,
                orderEvent.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to process order assignment for {OrderId}",
                orderEvent.OrderId);

            var failProducer = services.GetRequiredService<IProducer<string, OrderAssignmentFailedEvent>>();
            await failProducer.ProduceAsync("order-assignment-failed", new Message<string, OrderAssignmentFailedEvent>
            {
                Key = orderEvent.OrderId.ToString(),
                Value = new OrderAssignmentFailedEvent(
                    orderEvent.OrderId,
                    ex.Message,
                    DateTime.UtcNow)
            }, ct);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource.Cancel();
        _consumer?.Close();
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _cancellationTokenSource.Dispose();
        _consumer?.Dispose();
        base.Dispose();
    }
}