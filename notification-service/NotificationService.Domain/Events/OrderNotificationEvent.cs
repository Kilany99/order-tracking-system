
using NotificationService.Domain.Enums;

namespace NotificationService.Domain.Events;

public class OrderNotificationEvent
{
    public Guid OrderId { get; set; }
    public string CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public Guid? DriverId { get; set; }
    public string? DriverName { get; set; }
    public OrderStatus Status { get; set; }
    public decimal? TotalAmount { get; set; }
    public double? DeliveryLatitude { get; set; }
    public double? DeliveryLongitude { get; set; }
    public DateTime Timestamp { get; set; }
}