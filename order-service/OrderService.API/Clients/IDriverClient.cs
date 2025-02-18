using OrderService.Domain.Entities;
using OrderService.Infrastructure.Services;

namespace OrderService.API.Clients
{
    public interface IDriverClient
    {
        Task<Guid> AssignDriverAsync(double latitude, double longitude);
        Task<Guid> FindNearestDriverAsync(double latitude, double longitude);

    }

    public class HttpDriverClient : IDriverClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<HttpDriverClient> _logger;
        private readonly RedisCacheService _cacheService;

        public HttpDriverClient(
            HttpClient httpClient,
            ILogger<HttpDriverClient> logger,
            RedisCacheService cacheService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<Guid> AssignDriverAsync(double latitude, double longitude)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/drivers/assign", new
                {
                    Latitude = latitude,
                    Longitude = longitude,
                });

                response.EnsureSuccessStatusCode();

                return await response.Content.ReadFromJsonAsync<Guid>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Failed to assign driver");
                throw new Exception("No Available Drivers!");
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

    }

}