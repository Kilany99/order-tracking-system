﻿using Confluent.Kafka;
using OrderService.API.Models;
using OrderService.API.Serialization;
using OrderService.Infrastructure.Repositories;
using System.Text.Json;

namespace OrderService.API.Consumers;

public class DriverLocationConsumer : IHostedService
{
    private readonly IConsumer<string, DriverLocationEvent> _consumer;
    private readonly ILogger<DriverLocationConsumer> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    public DriverLocationConsumer(ILogger<DriverLocationConsumer> logger, IServiceScopeFactory scopeFactory,IConfiguration configuration)
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
                    using (var scope = _scopeFactory.CreateScope())  // ✅ Create a scope
                    {
                        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();  // Resolve scoped service


                        var orders = await orderRepository.GetOrdersNearLocationAsync(
                        location.Latitude,
                        location.Longitude,
                        1000 // 1km radius
                    );

                        foreach (var order in orders)
                        {
                            order.MarkAsOutForDelivery();
                            await orderRepository.UpdateAsync(order);
                            _logger.LogInformation("Order {OrderId} is out for delivery", order.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error consuming Kafka message");
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
}