using Confluent.Kafka;

namespace OrderService.API.Models;

public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public string CustomerId { get; set; }

    public double DeliveryLatitude { get; set; }
    public double DeliveryLongitude { get; set; }
    public DateTime CreatedAt { get; set; }

    public OrderCreatedEvent()
    {
    }

    public OrderCreatedEvent(
        Guid orderId,
        string customerId,
        double deliveryLatitude,
        double deliveryLongitude)
    {
        OrderId = orderId;
        CustomerId = customerId;
        DeliveryLatitude = deliveryLatitude;
        DeliveryLongitude = deliveryLongitude;
        CreatedAt = DateTime.UtcNow;
    }

    public bool IsValid()
    {
        return OrderId != Guid.Empty
            && !string.IsNullOrEmpty(CustomerId)
            && DeliveryLatitude != 0
            && DeliveryLongitude != 0;
    }
}

public record DriverAssignedEvent(
    Guid OrderId,
    Guid DriverId,
    DateTime AssignedAt);

public class DriverLocationEvent
{
    public Guid DriverId {  get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime TimeSpan { get; set; }

    public DriverLocationEvent() { }

    public DriverLocationEvent(Guid driverId, double latitude, double longitude,DateTime span)
    {
        DriverId = driverId;
        Latitude = latitude;
        Longitude = longitude;
        TimeSpan = span;
        
    }

}

public record OrderAssignmentFailedEvent(
    Guid OrderId,
    string Reason,
    DateTime FailedAt);