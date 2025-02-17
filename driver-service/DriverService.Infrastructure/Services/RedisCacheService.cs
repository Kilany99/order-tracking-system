
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace DriverService.Infrastructure.Services;

public class RedisCacheService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _database = redis.GetDatabase();
        _logger = logger;
    }

    public async Task CacheDriverLocationAsync(Guid driverId, double lat, double lon)
    {
        var key = $"driver:{driverId}:location";
        var value = $"{lat},{lon}";
        await _database.StringSetAsync(key, value, TimeSpan.FromMinutes(5));
    }

    public async Task<(double lat, double lon)?> GetCachedLocationAsync(Guid driverId)
    {
        var key = $"driver:{driverId}:location";
        var value = await _database.StringGetAsync(key);

        if (!value.HasValue) return null;

        var parts = value.ToString().Split(',');
        if (parts.Length != 2 ||
            !double.TryParse(parts[0], out var lat) ||
            !double.TryParse(parts[1], out var lon))
        {
            _logger.LogWarning("Invalid cached location format for {DriverId}", driverId);
            return null;
        }

        return (lat, lon);
    }
}