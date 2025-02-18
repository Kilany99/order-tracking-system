﻿
namespace DriverService.Domain.Events;

public class DriverAssignedEvent
{
    public Guid OrderId { get; }
    public Guid DriverId { get; }
    public DateTime Timestamp { get; }

    public DriverAssignedEvent(Guid orderId, Guid driverId, DateTime timestamp)
    {
        OrderId = orderId;
        DriverId = driverId;
        Timestamp = timestamp;
    }
}

public class OrderAssignmentFailedEvent
{
    public Guid OrderId { get; }
    public string Reason { get; }
    public DateTime Timestamp { get; }

    public OrderAssignmentFailedEvent(Guid orderId, string reason, DateTime timestamp)
    {
        OrderId = orderId;
        Reason = reason;
        Timestamp = timestamp;
    }
}
