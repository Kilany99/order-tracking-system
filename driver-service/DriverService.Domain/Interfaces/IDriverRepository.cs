using DriverService.Domain.Entities;
using Microsoft.EntityFrameworkCore.Storage;
using System.Threading.Tasks;

namespace DriverService.Domain.Interfaces;

 public interface IDriverRepository
{
    Task<Driver> GetByIdAsync(Guid id);
    Task<IEnumerable<Driver>> GetAvailableDriversAsync();
    Task AddAsync(Driver driver);
    Task UpdateAsync(Driver driver);
    Task DeleteAsync(Guid id);
    Task<bool> SaveChangesAsync();
    Task<bool> DriverExistsAsync(string email);
    Task AddAuthAsync(DriverAuth auth);
    Task SaveRefreshTokenAsync(Guid driverId, string token, DateTime expires);
    Task<RefreshToken> GetRefreshTokenAsync(string token);
    Task<DriverAuth> GetAuthByEmailAsync(string email);
    Task<Driver?> FindNearestDriverAsync(double latitude, double longitude);
    Task<Driver> AssignDriverAsync(Guid driverId, Guid orderId);
    Task<Driver?> IsOrderAssignedToAnyDriverAsync(Guid orderId);
    Task<DriverResponse> GetByOrderId(Guid orderId);

    Task<List<Driver>> GetDriversByIdsAsync(IEnumerable<Guid> driverIds);

    Task<IDbContextTransaction> BeginTransactionAsync();


}
