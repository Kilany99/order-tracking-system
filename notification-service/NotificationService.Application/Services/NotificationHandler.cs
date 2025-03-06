using Microsoft.Extensions.Logging;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Events;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Templates;
using NotificationService.Infrastructure.NotificationsMetrics;


namespace NotificationService.Application.Services;

public class NotificationHandler : INotificationHandler
{
    private readonly IEmailService _emailService;
    private readonly ICustomerService _customerService;
    private readonly ILogger<NotificationHandler> _logger;
    private readonly NotificationMetrics _metrics;

    public NotificationHandler(
        IEmailService emailService,
        ICustomerService customerService,
        ILogger<NotificationHandler> logger,
        NotificationMetrics metrics)
    {
        _emailService = emailService;
        _customerService = customerService;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task HandleOrderNotificationAsync(OrderNotificationEvent notification)
    {
        using var timer = _metrics.BeginNotificationProcessing();

        try
        {
            // Fetch customer info if email is missing
            if (string.IsNullOrEmpty(notification.CustomerEmail) && !string.IsNullOrEmpty(notification.CustomerId))
            {
                var customerInfo = await _customerService.GetCustomerInfoAsync(notification.CustomerId);
                notification.CustomerEmail = customerInfo.Email;
                notification.CustomerName = customerInfo.Name;
            }

            if (string.IsNullOrEmpty(notification.CustomerEmail))
            {
                _logger.LogWarning(
                    "Skipping notification for order {OrderId} - missing customer email",
                    notification.OrderId);
                return;
            }

            string subject;
            string body;
            (subject, body) = GetEmailContent(notification);

            await _emailService.SendEmailAsync(
                notification.CustomerEmail,
                subject,
                body);

            _logger.LogInformation(
                "Sent {Status} email notification to {Email} for order {OrderId}",
                notification.Status,
                notification.CustomerEmail,
                notification.OrderId);
            _metrics.RecordNotificationSent(true);
            _metrics.RecordMessageProcessed(notification.Status.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send {Status} email notification for order {OrderId}",
                notification.Status,
                notification.OrderId);
            _metrics.RecordNotificationSent(false);
            _metrics.RecordMessageError(notification.Status.ToString());
            throw;
        }
    }

    private (string subject, string body) GetEmailContent(OrderNotificationEvent notification)
    {
        return notification.Status switch
        {
            OrderStatus.Created => (
                "Order Confirmation",
                OrderEmailTemplates.OrderCreated(
                    notification.CustomerName ?? "Valued Customer",
                    notification.OrderId.ToString(),
                    notification.TotalAmount ?? 0)
            ),

            OrderStatus.Assigned => (
                "Driver Assigned to Your Order",
                OrderEmailTemplates.OrderAssigned(
                    notification.CustomerName ?? "Valued Customer",
                    notification.OrderId.ToString(),
                    notification.DriverName ?? "Your assigned driver")
            ),

            OrderStatus.Preparing => (
                "Your Order is Being Prepared",
                OrderEmailTemplates.OrderPreparing(
                    notification.CustomerName ?? "Valued Customer",
                    notification.OrderId.ToString())
            ),

            OrderStatus.OutForDelivery => (
                "Your Order is Out for Delivery",
                OrderEmailTemplates.OrderOutForDelivery(
                    notification.CustomerName ?? "Valued Customer",
                    notification.OrderId.ToString(),
                    notification.DriverName ?? "Your driver")
            ),

            OrderStatus.Delivered => (
                "Order Delivered Successfully",
                OrderEmailTemplates.OrderDelivered(
                    notification.CustomerName ?? "Valued Customer",
                    notification.OrderId.ToString())
            ),

            _ => throw new ArgumentException($"Unsupported order status: {notification.Status}")
        };
    }
}