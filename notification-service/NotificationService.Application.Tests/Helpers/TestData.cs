using NotificationService.Domain.Enums;
using NotificationService.Domain.Events;

namespace NotificationService.Application.Tests.Helpers;

public static class TestData
{
    public static OrderNotificationEvent CreateOrderCreatedEvent()
    {
        return new OrderNotificationEvent
        {
            OrderId = Guid.NewGuid(),
            CustomerId = "customer123",
            CustomerName = "Test Customer",
            CustomerEmail = "test@example.com",
            Status = OrderStatus.Created,
            Timestamp = DateTime.UtcNow
        };
    }

    public static OrderNotificationEvent CreateDriverAssignedEvent()
    {
        return new OrderNotificationEvent
        {
            OrderId = Guid.NewGuid(),
            CustomerId = "customer123",
            CustomerName = "Test Customer",
            CustomerEmail = "test@example.com",
            DriverId = Guid.NewGuid(),
            DriverName = "Test Driver",
            Status = OrderStatus.Assigned,
            Timestamp = DateTime.UtcNow
        };
    }
}
