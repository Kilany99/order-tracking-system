using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using OrderService.API.Clients;
using OrderService.API.Hubs;
using OrderService.API.Models;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.Routing.Interfaces;
using OrderService.Infrastructure.Serialization;
using OrderService.Infrastructure.Services;

namespace OrderService.API.Consumers;

public class DriverLocationConsumer : BackgroundService
{
    private readonly IConsumer<string, DriverLocationEvent> _consumer;
    private readonly ILogger<DriverLocationConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _topic;

    public DriverLocationConsumer(
        ILogger<DriverLocationConsumer> logger,
        IServiceScopeFactory scopeFactory,
        IConfiguration config)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _topic = config["Kafka:Topic"];

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = config["Kafka:BootstrapServers"],
            GroupId = "driver-location-group",
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = false,
            MaxPollIntervalMs = 300000,
            SessionTimeoutMs = 10000
        };

        _consumer = new ConsumerBuilder<string, DriverLocationEvent>(consumerConfig)
            .SetValueDeserializer(new JsonDeserializer<DriverLocationEvent>())
            .SetErrorHandler((_, e) => _logger.LogError($"Consumer error: {e.Reason}"))
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_topic);
        _logger.LogInformation($"Started driver location consumer for topic: {_topic}");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = _consumer.Consume(stoppingToken);
                    var location = result.Message.Value;

                    _logger.LogDebug($"Processing location update for driver {location.DriverId}");
                    await ProcessLocationUpdate(location);
                    _consumer.Commit(result);
                }
                catch (ConsumeException e)
                {
                    _logger.LogError(e, $"Consumer error: {e.Error.Reason}");
                    if (e.Error.IsFatal)
                        await Task.Delay(5000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Location processing failed");
                    await Task.Delay(1000, stoppingToken);
                }
            }
        }
        finally
        {
            _logger.LogInformation("Shutting down driver location consumer");
            _consumer.Close();
        }
    }

    private async Task ProcessLocationUpdate(DriverLocationEvent location)
    {
        using var scope = _scopeFactory.CreateScope();
        var services = scope.ServiceProvider;

        var orderRepo = services.GetRequiredService<IOrderRepository>();
        var hubContext = services.GetRequiredService<IHubContext<TrackingHub>>();
        var driverClient = services.GetRequiredService<IDriverClient>();
        var cacheService = services.GetRequiredService<RedisCacheService>();
        var routingService = services.GetRequiredService<IRoutingService>();

        try
        {
            // 1. Update driver location in cache
            await cacheService.CacheDriverLocationAsync(location.DriverId, location.Latitude, location.Longitude);


            // 2. Update real-time tracking
            var assignedOrders = await driverClient.GetActiveOrdersByDriver(location.DriverId);
            if (assignedOrders != null)
            {
                foreach (var order in assignedOrders)
                {
                    // Get ETA
                    var eta = await routingService.CalculateETA(
                        location.Latitude,
                        location.Longitude,
                        order.CurrentLat,
                        order.CurrentLon
                    );
                    // Get full route
                    var route = await routingService.GetRoute(
                        location.Latitude,
                        location.Longitude,
                        order.CurrentLat,
                        order.CurrentLon
                    );

                    await hubContext.Clients.Group(order.OrderId.ToString())
                        .SendAsync("DriverLocationUpdate", new
                        {
                            orderId = order.OrderId,
                            driverId = location.DriverId,
                            currentLocation = new
                            {
                                lat = location.Latitude,
                                lng = location.Longitude
                            },
                            eta = eta.TotalMinutes,
                            estimatedArrival = route.EstimatedArrival,
                            remainingDistance = route.Distance,
                            routePoints = route.RoutePoints
                            
                        });
                }
            }

            //3. Handle automatic assignments
            if (await driverClient.IsDriverAvailable(location.DriverId))
            {
                var pendingOrders = await orderRepo.GetOrdersNearLocationAsync(
                    location.Latitude,
                    location.Longitude,
                    5000);

                foreach (var order in pendingOrders)
                {
                    if (await orderRepo.TryAssignDriver(order.Id, location.DriverId))
                    {
                        await hubContext.Clients.Group(order.Id.ToString())
                            .SendAsync("OrderAssigned", new
                            {
                                orderId = order.Id,
                                driverId = location.DriverId,
                                position = new { lat = location.Latitude, lng = location.Longitude }
                            });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed processing location for driver {location.DriverId}");
        }
    }
}