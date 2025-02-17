
namespace DriverService.Core.Dtos;

public record DriverLocationResponse(
    Guid DriverId,
    double Latitude,
    double Longitude,
    DateTime LastUpdated);