using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.DTOs;
using NotificationService.Domain.Exceptions;
using NotificationService.Domain.Interfaces;
using System.Net;
using System.Net.Http.Json;


namespace NotificationService.Infrastructure.Services;

public class CustomerService : ICustomerService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<CustomerService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Add(
             "X-Service-ApiKey",
             _configuration["Services:OrderService:ApiKey"]);
        _httpClient.BaseAddress = new Uri(_configuration["Services:OrderService:BaseUrl"]);
    }

    public async Task<CustomerInfo> GetCustomerInfoAsync(string customerId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/customerinfo/{customerId}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new NotFoundException($"Customer not found with ID: {customerId}");
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<CustomerInfo>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling customer info service for {CustomerId}", customerId);
            throw new ServiceException("Error communicating with customer service", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing customer info for {CustomerId}", customerId);
            throw;
        }
    }
}