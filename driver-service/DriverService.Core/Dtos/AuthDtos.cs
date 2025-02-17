
namespace DriverService.Core.Dtos;

public record AuthResponse(
    Guid DriverId,
    string AccessToken,
    string RefreshToken,
    DateTime Expiration);