using DriverService.Domain.Entities;
using DriverService.Domain.Interfaces;
using DriverService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;


namespace DriverService.Infrastructure.Repositories
{
    public class DriverRepository : IDriverRepository
    {
        private readonly DriverDbContext _context;

        public DriverRepository(DriverDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
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
    }
}
