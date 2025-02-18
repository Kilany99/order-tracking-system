using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using OrderService.API.Clients;
using OrderService.API.Hubs;
using OrderService.API.Models;
using OrderService.API.Serialization;
using OrderService.Infrastructure.Repositories;

namespace OrderService.API.Consumers;

public class DriverLocationConsumer : IHostedService
{
    private readonly IConsumer<string, DriverLocationEvent> _consumer;
    private readonly ILogger<DriverLocationConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    public DriverLocationConsumer(ILogger<DriverLocationConsumer> logger, IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["kafka:BootstrapServers"],
            GroupId = "order-service",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _consumer = new ConsumerBuilder<string, DriverLocationEvent>(config)
            .SetValueDeserializer(new JsonDeserializer<DriverLocationEvent>())
            .Build();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _consumer.Subscribe(_configuration["kafka:Topic"]);

        Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(cancellationToken);
                    var location = result.Message.Value;

                    _logger.LogInformation(
                        "Received driver location: DriverId={DriverId}, Lat={Lat}, Lng={Lng}",
                        location.DriverId,
                        location.Latitude,
                        location.Longitude
                    );
                    await ProcessLocationUpdate(result.Message.Value);
                    _consumer.Commit(result);

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing location update");
                    await Task.Delay(1000);
                }
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _consumer.Close();
        return Task.CompletedTask;
    }

    private async Task ProcessLocationUpdate(DriverLocationEvent location)
    {
        using var scope = _scopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        var orderRepository = services.GetRequiredService<IOrderRepository>();
        var hubContext = services.GetRequiredService<IHubContext<TrackingHub>>();
        var driverClient = services.GetRequiredService<IDriverClient>();

        try
        {
            // Update driver position in Driver Service
            await driverClient.AssignDriverAsync(
                location.Latitude,
                location.Longitude);

            // Get nearby pending orders
            var orders = await orderRepository.GetOrdersNearLocationAsync(
                location.Latitude,
                location.Longitude,
                1000);

            foreach (var order in orders)
            {
                if (await orderRepository.TryAssignDriver(order.Id, location.DriverId))
                {
                    _logger.LogInformation("Assigned order {OrderId} to driver {DriverId}",
                        order.Id, location.DriverId);

                    // Notify clients
                    await hubContext.Clients
                        .Group(order.Id.ToString())
                        .SendAsync("OrderAssigned", new
                        {
                            orderId = order.Id,
                            driverId = location.DriverId,
                            position = new
                            {
                                lat = location.Latitude,
                                lng = location.Longitude
                            }
                        });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing location for driver {DriverId}",
                location.DriverId);
        }
    }
}