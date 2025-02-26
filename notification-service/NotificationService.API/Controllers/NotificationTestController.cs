using Microsoft.AspNetCore.Mvc;
using NotificationService.Domain.Events;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Enums;

namespace NotificationService.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationTestController : ControllerBase
{
    private readonly INotificationHandler _notificationHandler;
    private readonly ILogger<NotificationTestController> _logger;

    public NotificationTestController(
        INotificationHandler notificationHandler,
        ILogger<NotificationTestController> logger)
    {
        _notificationHandler = notificationHandler;
        _logger = logger;
    }

    [HttpPost("test-order-notification")]
    public async Task<IActionResult> TestOrderNotification([FromBody] TestNotificationRequest request)
    {
        try
        {
            var notification = new OrderNotificationEvent
            {
                OrderId = request.OrderId,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                Status = request.Status,
                DriverName = request.DriverName,
                DriverId = request.DriverId,
                TotalAmount = request.TotalAmount,
                Timestamp = DateTime.UtcNow
            };

            await _notificationHandler.HandleOrderNotificationAsync(notification);

            return Ok(new
            {
                message = $"Test notification sent successfully for order {request.OrderId}",
                status = request.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test notification");
            return StatusCode(500, new
            {
                error = "Failed to send test notification",
                details = ex.Message
            });
        }
    }

    [HttpPost("test-all-notifications")]
    public async Task<IActionResult> TestAllNotifications([FromBody] BasicTestRequest request)
    {
        try
        {
            var orderId = request.OrderId;
            var results = new List<string>();

            // Test Order Created
            await SendTestNotification(new OrderNotificationEvent
            {
                OrderId = orderId,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                Status = OrderStatus.Created,
                TotalAmount = request.TotalAmount,
                Timestamp = DateTime.UtcNow
            }, results);

            // Test Driver Assigned
            await SendTestNotification(new OrderNotificationEvent
            {
                OrderId = orderId,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                Status = OrderStatus.Assigned,
                DriverName = "Test Driver",
                DriverId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow
            }, results);

            // Test Order Preparing
            await SendTestNotification(new OrderNotificationEvent
            {
                OrderId = orderId,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                Status = OrderStatus.Preparing,
                Timestamp = DateTime.UtcNow
            }, results);

            // Test Order Out for Delivery
            await SendTestNotification(new OrderNotificationEvent
            {
                OrderId = orderId,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                Status = OrderStatus.OutForDelivery,
                DriverName = "Test Driver",
                Timestamp = DateTime.UtcNow
            }, results);

            // Test Order Delivered
            await SendTestNotification(new OrderNotificationEvent
            {
                OrderId = orderId,
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                Status = OrderStatus.Delivered,
                Timestamp = DateTime.UtcNow
            }, results);

            return Ok(new
            {
                message = "All test notifications completed",
                results = results
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in test all notifications");
            return StatusCode(500, new
            {
                error = "Failed to complete all test notifications",
                details = ex.Message
            });
        }
    }

    private async Task SendTestNotification(OrderNotificationEvent notification, List<string> results)
    {
        try
        {
            await _notificationHandler.HandleOrderNotificationAsync(notification);
            results.Add($"Successfully sent {notification.Status} notification");
        }
        catch (Exception ex)
        {
            results.Add($"Failed to send {notification.Status} notification: {ex.Message}");
            throw;
        }
    }
}

public class TestNotificationRequest
{
    public Guid OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public string? DriverName { get; set; }
    public Guid? DriverId { get; set; }
    public decimal? TotalAmount { get; set; }
}

public class BasicTestRequest
{
    public Guid OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}