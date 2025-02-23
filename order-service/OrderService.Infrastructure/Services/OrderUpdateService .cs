using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Entities;
using OrderService.Domain.Interfaces;
using OrderService.Infrastructure.Hubs;

namespace OrderService.Infrastructure.Services;
public class OrderUpdateService : IOrderUpdateService
{
    private readonly IHubContext<TrackingHub> _hubContext;
    private readonly ILogger<OrderUpdateService> _logger;

    public OrderUpdateService(
        IHubContext<TrackingHub> hubContext,
        ILogger<OrderUpdateService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendOrderStatusUpdate(Guid orderId, OrderStatus status, Guid? driverId = null)
    {
        try
        {
            _logger.LogInformation(
                "Sending order status update for OrderId: {OrderId}, Status: {Status}, DriverId: {DriverId}",
                orderId, status, driverId);

            await _hubContext.Clients.Group(orderId.ToString())
                .SendAsync("OrderStatusUpdated", new
                {
                    orderId = orderId,
                    status = (int)status,
                    driverId = driverId,
                    timestamp = DateTime.UtcNow
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending order status update for OrderId: {OrderId}",
                orderId);
            throw;
        }
    }
}