using DriverService.Domain.Entities;

namespace DriverService.Domain.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(Driver driver, DriverAuth auth);

        RefreshToken GenerateRefreshToken();

    }
}
