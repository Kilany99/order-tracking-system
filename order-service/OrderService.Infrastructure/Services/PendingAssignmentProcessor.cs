using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Domain.Models;
using OrderService.Infrastructure.Repositories;

using System.Threading.Channels;

namespace OrderService.Infrastructure.Services;

public class PendingAssignmentProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<PendingAssignmentProcessor> logger,
    IOrderProcessingChannel<OrderCreatedEvent> orderChannel) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<PendingAssignmentProcessor> _logger = logger;
    private readonly IOrderProcessingChannel<OrderCreatedEvent> _orderChannel = orderChannel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("PendingAssignmentProcessor service started.");
                using var scope = _scopeFactory.CreateScope();
                var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

                var pendingAttempts = await orderRepository.GetOrdersWithPendingAssignmentAsync();
                foreach (var attempt in pendingAttempts)
                {
                    if (attempt.NextAssignmentAttempt <= DateTime.UtcNow)
                    {
                        var order = await orderRepository.GetByIdAsync(attempt.Id);
                        if (order != null && order.Status == OrderStatus.Created)
                        {
                            _logger.LogInformation("Found order with id {orderId} still pending and re-proccessing it...",order.Id);
                            // Re-queue the order for processing
                            await _orderChannel.Writer.WriteAsync(new OrderCreatedEvent(
                                order.Id,
                                order.CustomerId,
                                order.DeliveryLatitude,
                                order.DeliveryLongitude
                            ), stoppingToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending assignments");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping PendingAssignmentProcessor...");
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}