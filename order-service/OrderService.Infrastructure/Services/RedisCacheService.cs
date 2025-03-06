
using Microsoft.Extensions.Logging;
using OrderService.Infrastructure.Metrics;
using StackExchange.Redis;
using System.Text.Json;

namespace OrderService.Infrastructure.Services;

public class RedisCacheService
{
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly OrderMetrics _metrics;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger,
        OrderMetrics metrics)
    {
        _database = redis.GetDatabase();
        _logger = logger;
        _metrics = metrics;

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

        _metrics.RecordCacheAccess(true);

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


    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            if (!value.HasValue)
                return default;

            return JsonSerializer.Deserialize<T>(value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting value from cache for key {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var serializedValue = JsonSerializer.Serialize(value);
            await _database.StringSetAsync(key, serializedValue, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting value in cache for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing key {Key} from cache", key);
        }
    }
}