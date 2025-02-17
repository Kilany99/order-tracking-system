using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using OrderService.API.Clients;
using OrderService.API.Models;
using OrderService.API.Serialization;
using System.Text.Json;

namespace OrderService.API.Consumers;

public class OrderAssignmentConsumer : BackgroundService
{
    private readonly IConsumer<string, OrderCreatedEvent> _consumer;
    private readonly ILogger<OrderAssignmentConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public OrderAssignmentConsumer(
        ILogger<OrderAssignmentConsumer> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration config)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"],
            GroupId = "order-service-assignment",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, OrderCreatedEvent>(consumerConfig)
            .SetValueDeserializer(new JsonDeserializer<OrderCreatedEvent>())
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe("orders-created");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(stoppingToken);
                await ProcessOrderAssignment(result.Message.Value, stoppingToken);
                _consumer.Commit(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order assignment");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task ProcessOrderAssignment(
        OrderCreatedEvent orderEvent,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        var driverClient = services.GetRequiredService<IDriverClient>();
        var producer = services.GetRequiredService<IProducer<string, DriverAssignedEvent>>();

        try
        {
            var driverId = await driverClient.AssignDriverAsync(
                orderEvent.DeliveryLatitude,
                orderEvent.DeliveryLongitude);

            await producer.ProduceAsync("drivers-assigned", new Message<string, DriverAssignedEvent>
            {
                Key = orderEvent.OrderId.ToString(),
                Value = new DriverAssignedEvent(
                    orderEvent.OrderId,
                    driverId,
                    DateTime.UtcNow)
            }, cancellationToken);

            _logger.LogInformation("Assigned driver {DriverId} to order {OrderId}",
                driverId, orderEvent.OrderId);
        }
        catch (Exception)
        {
            _logger.LogWarning("No available drivers for order {OrderId}", orderEvent.OrderId);
            _logger.LogWarning("No available drivers for order {OrderId}", orderEvent.OrderId);

            var failProducer = services.GetRequiredService<IProducer<string, OrderAssignmentFailedEvent>>();

            await failProducer.ProduceAsync("order-assignment-failed", new Message<string, OrderAssignmentFailedEvent>
            {
                Key = orderEvent.OrderId.ToString(),
                Value = new OrderAssignmentFailedEvent(
                    orderEvent.OrderId,
                    "No available drivers",
                    DateTime.UtcNow)
            }, cancellationToken);
        }
    }

    public override void Dispose()
    {
        _consumer.Close();
        base.Dispose();
    }
}