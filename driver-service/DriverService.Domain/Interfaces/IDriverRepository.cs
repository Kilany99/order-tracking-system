using DriverService.Domain.Entities;

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



}
