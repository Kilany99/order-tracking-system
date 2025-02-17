
namespace DriverService.Core.Dtos;
public record DriverResponse(
    Guid Id,
    string Name,
    string VehicleType,
    bool IsAvailable);