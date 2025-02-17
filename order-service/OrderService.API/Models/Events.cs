namespace OrderService.API.Models;

public record OrderCreatedEvent(
        Guid OrderId,
        double DeliveryLatitude,
        double DeliveryLongitude,
        DateTime CreatedAt);

public record DriverAssignedEvent(
    Guid OrderId,
    Guid DriverId,
    DateTime AssignedAt);

public record DriverLocationEvent(
    Guid DriverId,
    double Latitude,
    double Longitude,
    DateTime Timestamp);

public record OrderAssignmentFailedEvent(
    Guid OrderId,
    string Reason,
    DateTime FailedAt);