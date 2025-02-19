
namespace OrderService.Domain.Entities;

public record OrderCreatedEvent(
        Guid OrderId,
        double DeliveryLatitude,
        double DeliveryLongitude,
        DateTime CreatedAt);

