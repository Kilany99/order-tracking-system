using Microsoft.Extensions.Logging;
using OrderService.Domain.Models;
using OrderService.Infrastructure.Services;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace OrderService.Infrastructure.Clients
{
    public interface IDriverClient
    {
        Task<Guid> AssignDriverToOrderAsync(double latitude, double longitude,Guid orderId);
        Task<Guid> FindNearestDriverAsync(double latitude, double longitude);
        Task UpdateDriverPosition(Guid driverId, double lat, double lng);
        Task<DriverLocation?> GetDriverLocation(Guid orderId);
        Task<IEnumerable<DriverOrderResponse>> GetActiveOrdersByDriver(Guid driverId);
        Task<bool> IsDriverAvailable(Guid driverId);
    }

    public class HttpDriverClient : IDriverClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpDriverClient> _logger;
        private readonly RedisCacheService _cacheService;
        private readonly ConcurrentDictionary<string, DriverLocation> _positions = new();

        public HttpDriverClient(
            HttpClient httpClient,
            ILogger<HttpDriverClient> logger,
            RedisCacheService cacheService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<Guid> AssignDriverToOrderAsync(double latitude, double longitude, Guid orderId)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/drivers/assign", new
                {
                    Latitude = latitude,
                    Longitude = longitude,
                    OrderId = orderId
                });

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<Guid>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to assign driver");
                throw;
            }
        }
        public async Task<Guid> FindNearestDriverAsync(double latitude, double longitude)
        {
            try
            {
                var response = await _httpClient.GetAsync(
                    $"/api/drivers/nearest?lat={latitude}&lon={longitude}");

                return await response.Content.ReadFromJsonAsync<Guid>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to find any drivers");
                throw new Exception("Failed to find any drivers");


            }
        }


    public async Task UpdateDriverPosition(Guid driverId, double lat, double lng)
        {
            // Get driver details from API
            var response = await _httpClient.GetAsync($"/api/drivers/{driverId}");
            if (!response.IsSuccessStatusCode)
            {
                // Optionally handle error (e.g. log and exit)
                _logger.LogWarning("Failed to retrieve driver details for driver {DriverId}", driverId);
                return;
            }

            // Deserialize the response into a DriverDto
            var json = await response.Content.ReadAsStringAsync();
            var driver = JsonSerializer.Deserialize<DriverDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (driver == null)
            {
                _logger.LogWarning("Driver details are null for driver {DriverId}", driverId);
                return;
            }

            // Only update position if the driver is InDelivery
            if (driver.Status == DriverStatus.InDelivery)
            {
                // Update local position cache
                _positions[driverId.ToString()] = new DriverLocation(driverId, lat, lng, DateTime.UtcNow);

                // Package location data into an object
                var locationData = new
                {
                    Latitude = lat,
                    Longitude = lng,
                    Timestamp = DateTime.UtcNow
                };

                // Post the updated location
                var postResponse = await _httpClient.PostAsJsonAsync($"/api/drivers/{driverId}/location", locationData);
                if (!postResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to update location for driver {DriverId}", driverId);
                }
            }
        }

        public async Task<DriverLocation?> GetDriverLocation(Guid orderId)
        {
            try
            {
                _logger.LogInformation("Getting driver associated with order Id {OrderId}...", orderId);

                // Get driver ID by order ID
                var driverResponse = await _httpClient.GetAsync($"/api/drivers/get-by-orderid?orderId={orderId}");

                if (!driverResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to retrieve driver for order {OrderId}. Status: {StatusCode}",
                        orderId, driverResponse.StatusCode);
                    return null;
                }

                var driver = await driverResponse.Content.ReadFromJsonAsync<DriverResponse>();

                if (driver?.Id == null || driver.Id == Guid.Empty)
                {
                    _logger.LogWarning("Invalid or empty driver ID received for order {OrderId}", orderId);
                    return null;
                }

                _logger.LogInformation("Getting driver location for Id {DriverId}...", driver.Id);

                // Get driver location
                var locationResponse = await _httpClient.GetAsync($"/api/drivers/{driver.Id}/location");

                if (!locationResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to retrieve location for driver {DriverId}. Status: {StatusCode}",
                        driver.Id, locationResponse.StatusCode);
                    return null;
                }

                var location = await locationResponse.Content.ReadFromJsonAsync<DriverLocation>();
                return location;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving driver location for order {OrderId}", orderId);
                return null;
            }
        }
        public async Task<IEnumerable<DriverOrderResponse>> GetActiveOrdersByDriver(Guid driverId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/drivers/{driverId}/active-orders");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<IEnumerable<DriverOrderResponse>>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting active orders for driver {driverId}");
                return Enumerable.Empty<DriverOrderResponse>();
            }
        }

        public async Task<bool> IsDriverAvailable(Guid driverId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/drivers/{driverId}/availability");
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<DriverAvailabilityResponse>();
                return result?.IsAvailable ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking availability for driver {driverId}");
                return false;
            }
        }

        private record DriverAvailabilityResponse(bool IsAvailable);

        private enum DriverStatus { Offline, Assigned ,Available, InDelivery }
        private class DriverDto
        {
            public Guid Id { get; set; }
            public DriverStatus Status { get; set; }
        }

    }



}