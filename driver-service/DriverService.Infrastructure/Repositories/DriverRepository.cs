using DnsClient.Internal;
using DriverService.Domain.Entities;
using DriverService.Domain.Exceptions;
using DriverService.Domain.Interfaces;
using DriverService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using NpgsqlTypes;
using StackExchange.Redis;


namespace DriverService.Infrastructure.Repositories
{
    public class DriverRepository : IDriverRepository
    {
        private readonly DriverDbContext _context;
        private readonly ILogger<DriverRepository> _logger;
        public DriverRepository(DriverDbContext context, ILogger<DriverRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger;
        }

        public async Task<Driver> GetByIdAsync(Guid id)
        {
            return await _context.Drivers
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public async Task<IEnumerable<Driver>> GetAvailableDriversAsync()
        {
            return await _context.Drivers
                .Where(d => d.IsAvailable)
                .ToListAsync();
        }

        public async Task AddAsync(Driver driver)
        {
            await _context.Drivers.AddAsync(driver);
        }

        public async Task UpdateAsync(Driver driver)
        {
            _context.Drivers.Update(driver);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Guid id)
        {
            var driver = await GetByIdAsync(id) ?? throw new KeyNotFoundException("driver with given Id not found");

            _context.Drivers.Remove(driver);
        }

        public async Task<bool> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync() >= 0;
        }

        public async Task<bool> DriverExistsAsync(string email)
        {
            return await _context.DriverAuths
                .AnyAsync(a => a.Email == email);
        }

        public async Task AddAuthAsync(DriverAuth auth)
        {
            await _context.DriverAuths.AddAsync(auth);
        }

        public async Task SaveRefreshTokenAsync(Guid driverId, string token, DateTime expires)
        {
            var refreshToken = new RefreshToken
            {
                DriverId = driverId,
                Token = token,
                Expires = expires
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();
        }

        public async Task<RefreshToken> GetRefreshTokenAsync(string token)
        {

            return await _context.RefreshTokens
                .Include(rt => rt.Driver)
                .FirstOrDefaultAsync(rt => rt.Token == token) ??
                throw new KeyNotFoundException("Token not found!");
        }

        public async Task<DriverAuth> GetAuthByEmailAsync(string email)
        {
            return await _context.DriverAuths
                .Include(a => a.Driver)
                .FirstOrDefaultAsync(a => a.Email == email) ??
                throw new KeyNotFoundException("Not found for given email!");
        }


        public async Task<Driver?> FindNearestDriverAsync(double latitude, double longitude)
        {
            var drivers = await GetAvailableDriversAsync();

            if (!drivers.Any())
            {
                _logger.LogError("No available drivers found.");
                throw new NoAvailableDriversException();
            }

            var nearestDriver = drivers
                .AsParallel()
                .Select(d => new
                {
                    Driver = d,
                    Distance = CalculateDistance(d.CurrentLatitude, d.CurrentLongitude, latitude, longitude)
                })
                .OrderBy(x => x.Distance)
                .FirstOrDefault();

            if (nearestDriver == null)
            {
                _logger.LogWarning("No suitable driver found.");
                return null;
            }

            _logger.LogInformation("Selected driver {DriverId} at {Distance}m", nearestDriver.Driver.Id, nearestDriver.Distance);
            return nearestDriver.Driver;
        }



        public async Task<Driver> AssignDriverAsync(Guid driverId, Guid orderId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                _logger.LogInformation($"Trying to assing driver Id {driverId} to the order Id {orderId} from data layer");
                var driver = await _context.Drivers
                    .FirstOrDefaultAsync(d => d.Id == driverId && d.IsAvailable);

                if (driver == null) throw new DriverNotFoundException(driverId);

                driver.IsAvailable = false;
                driver.CurrentOrderId = orderId;
                driver.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return driver;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<Driver?> IsOrderAssignedToAnyDriverAsync(Guid orderId) =>
            await _context.Drivers.FirstOrDefaultAsync(d => d.CurrentOrderId == orderId);

        public async Task<DriverResponse> GetByOrderId(Guid orderId)
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.CurrentOrderId == orderId) ??
                throw new Exception("Driver not found for give order id {orderId}");
            return new DriverResponse(driver.Id,driver.Name,driver.VehicleType,driver.IsAvailable);
        }

        public async Task<List<Driver>> GetDriversByIdsAsync(IEnumerable<Guid> driverIds)
        {
            return await _context.Drivers
                .AsNoTracking()
                .Where(d => driverIds.Contains(d.Id))
                .ToListAsync();
        }
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }
        private double CalculateDistance(
      double lat1, double lon1,
      double lat2, double lon2)
        {
            const double R = 6371e3; // Earth radius in meters
            var φ1 = lat1 * Math.PI / 180;
            var φ2 = lat2 * Math.PI / 180;
            var Δφ = (lat2 - lat1) * Math.PI / 180;
            var Δλ = (lon2 - lon1) * Math.PI / 180;

            var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                    Math.Cos(φ1) * Math.Cos(φ2) *
                    Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return R * c;
        }

    }
}
