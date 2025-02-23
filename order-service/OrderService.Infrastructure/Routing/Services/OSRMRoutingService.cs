
using Confluent.Kafka;
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
    private readonly ITimeProvider _timeProvider;


    public OSRMRoutingService(
        HttpClient httpClient,
        ILogger<OSRMRoutingService> logger,
        IConfiguration configuration,
        IMemoryCache cache,
        ITimeProvider timeProvider = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
        _cache = cache;
        _timeProvider = timeProvider ?? new SystemTimeProvider();
        _httpClient.BaseAddress = new Uri(_configuration["OSRM:BaseUrl"] ?? "http://router.project-osrm.org");
    }

    public async Task<RouteResponse> GetRoute(double startLat, double startLng, double endLat, double endLng)
    {
        var cacheKey = $"route_{startLat}_{startLng}_{endLat}_{endLng}";

        if (_cache.TryGetValue(cacheKey, out RouteResponse cachedRoute))
        {
            return cachedRoute;
        }

        try
        {
            var requestUrl = $"/route/v1/driving/{startLng},{startLat};{endLng},{endLat}";
            requestUrl += "?overview=full&geometries=geojson&steps=true";

            _logger.LogInformation("Requesting route: {Url}", requestUrl);

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
                RoutePoints = ConvertCoordinates(jsonResponse.Routes[0].Geometry.Coordinates),
                EstimatedArrival = DateTime.UtcNow.AddSeconds(jsonResponse.Routes[0].Duration)
            };

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

    private List<Coordinate> ConvertCoordinates(List<List<double>> coordinates)
    {
        return coordinates.Select(coord => new Coordinate
        {
            // GeoJSON format is [longitude, latitude]
            Latitude = coord[1],
            Longitude = coord[0]
        }).ToList();
    }

    public async Task<TimeSpan> CalculateETA(double startLat, double startLng, double endLat, double endLng)
    {
        try
        {
            var route = await GetRoute(startLat, startLng, endLat, endLng);
            double trafficFactor = await GetTrafficFactor(startLat, startLng, endLat, endLng);
            var etaSeconds = route.Duration * trafficFactor;
            return TimeSpan.FromSeconds(etaSeconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating ETA");
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
        var hour = _timeProvider.Now.Hour;
        return (hour >= 7 && hour <= 9) || (hour >= 16 && hour <= 18) ? 1.5 : 1.0;
    }
}

public interface ITimeProvider
{
    DateTime Now { get; }
}

public class SystemTimeProvider : ITimeProvider
{
    public DateTime Now => DateTime.Now;
}