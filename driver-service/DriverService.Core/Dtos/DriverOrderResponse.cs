
namespace DriverService.Core.Dtos;

public record DriverOrderResponse(
    Guid OrderId,
    string DeliveryAddress,
    double CurrentLat, 
    double CurrentLon,
    string Status,
    DateTime? AssignedAt
);