using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using OrderService.API.Clients;
using OrderService.API.Hubs;
using OrderService.API.Models;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.Serialization;

namespace OrderService.API.Consumers;

public class DriverLocationConsumer : BackgroundService
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe("driver-location-updates");
        _logger.LogInformation("driver-location-updates consumer is running");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(stoppingToken);
                    var location = result.Message.Value;

                    _logger.LogInformation(
                        "Received driver location: DriverId={DriverId}, Lat={Lat}, Lng={Lng}",
                        location.DriverId,
                        location.Latitude,
                        location.Longitude
                    );

                    await ProcessLocationUpdate(location);
                    _consumer.Commit(result);
                }
                catch (ConsumeException e)
                {
                    _logger.LogError(e, "Consume error");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing location update");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }


    private async Task ProcessLocationUpdate(DriverLocationEvent location)
    {
        using var scope = _scopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        var orderRepository = services.GetRequiredService<IOrderRepository>();
        var hubContext = services.GetRequiredService<IHubContext<TrackingHub>>();
        var driverClient = services.GetRequiredService<IDriverClient>();
        _logger.LogInformation("////ProcessLocationUpdate////");

        try
        {
            // 1. Get all orders assigned to this driver
            var assignedOrders = await orderRepository.GetOrdersByDriver(location.DriverId);
            _logger.LogInformation("complete GetOrdersByDriver");

            // 2. Broadcast to connected clients
            foreach (var order in assignedOrders)
            {
                await hubContext.Clients.Group(order.Id.ToString())
                    .SendAsync("DriverLocationUpdate", new
                    {
                        orderId = order.Id,
                        driverId = location.DriverId,
                        lat = location.Latitude,
                        lng = location.Longitude
                    });
            }
            _logger.LogInformation("complete hubContext");


            // Update assigned order position to driver in Driver Service
            await driverClient.AssignDriverAsync(
                location.Latitude,
                location.Longitude);
            _logger.LogInformation("complete AssignDriverAsync");

            // Get nearby pending orders
            var orders = await orderRepository.GetOrdersNearLocationAsync(
                location.Latitude,
                location.Longitude,
                1000);
            _logger.LogInformation("complete GetOrdersNearLocationAsync");

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