
namespace OrderService.Application.Responses;

public record DriverOrderResponse(
    Guid OrderId,
    string DeliveryAddress,
    double CurrentLat,
    double CurrentLon,
    string Status,
    DateTime? AssignedAt
);