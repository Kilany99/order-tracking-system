
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OrderService.Infrastructure.Routing.Interfaces;
using OrderService.Infrastructure.Routing.Models;
using System.Net.Http.Json;

namespace OrderService.Infrastructure.Routing.Services;

public class OSRMRoutingService : IRoutingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OSRMRoutingService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    public OSRMRoutingService(
        HttpClient httpClient,
        ILogger<OSRMRoutingService> logger,
        IConfiguration configuration,
        IMemoryCache cache)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _cache = cache;

        // Configure base address for OSRM service
        _httpClient.BaseAddress = new Uri(_configuration["OSRM:BaseUrl"] ?? "http://router.project-osrm.org");
    }

    public async Task<TimeSpan> CalculateETA(double startLat, double startLng, double endLat, double endLng)
    {
        try
        {
            var route = await GetRoute(startLat, startLng, endLat, endLng);

            // Add traffic factor (you can adjust this based on time of day, historical data, etc.)
            double trafficFactor = await GetTrafficFactor(startLat, startLng, endLat, endLng);

            // Calculate ETA considering traffic
            var etaSeconds = route.Duration * trafficFactor;
            return TimeSpan.FromSeconds(etaSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating ETA");
            throw;
        }
    }

    public async Task<RouteResponse> GetRoute(double startLat, double startLng, double endLat, double endLng)
    {
        var cacheKey = $"route_{startLat}_{startLng}_{endLat}_{endLng}";

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out RouteResponse cachedRoute))
        {
            return cachedRoute;
        }

        try
        {
            // Format: /route/v1/driving/{startLng},{startLat};{endLng},{endLat}
            var requestUrl = $"/route/v1/driving/{startLng},{startLat};{endLng},{endLat}";
            requestUrl += "?overview=full&geometries=geojson&steps=true";

            var response = await _httpClient.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();

            var jsonResponse = await response.Content.ReadFromJsonAsync<OSRMResponse>();

            if (jsonResponse?.Routes == null || !jsonResponse.Routes.Any())
            {
                throw new Exception("No route found");
            }

            var route = new RouteResponse
            {
                Distance = jsonResponse.Routes[0].Distance,
                Duration = jsonResponse.Routes[0].Duration,
                RoutePoints = DecodeGeometry(jsonResponse.Routes[0].Geometry),
                EstimatedArrival = DateTime.UtcNow.AddSeconds(jsonResponse.Routes[0].Duration)
            };

            // Cache the route for 5 minutes
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));

            _cache.Set(cacheKey, route, cacheOptions);

            return route;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting route");
            throw;
        }
    }

    public async Task<double> CalculateDistance(double startLat, double startLng, double endLat, double endLng)
    {
        var route = await GetRoute(startLat, startLng, endLat, endLng);
        return route.Distance;
    }

    private async Task<double> GetTrafficFactor(double startLat, double startLng, double endLat, double endLng)
    {
        // This is a simplified example. In a real application, you might:
        // 1. Use historical traffic data
        // 2. Consider time of day
        // 3. Check real-time traffic APIs
        // 4. Use machine learning predictions

        var hour = DateTime.Now.Hour;

        // Simple peak hours traffic factor
        if ((hour >= 7 && hour <= 9) || (hour >= 16 && hour <= 18))
        {
            return 1.5; // 50% longer during peak hours
        }

        return 1.0;
    }

    private List<Coordinate> DecodeGeometry(string geometry)
    {
        // Implement polyline decoding logic here
        // This is a simplified version
        return new List<Coordinate>();
    }
}
