
namespace OrderService.Domain.Entities;

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