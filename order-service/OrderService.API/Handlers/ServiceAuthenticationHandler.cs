using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace OrderService.API.Handlers;


// Helper service for service to service authentication within the container using JWT tokens

public class ServiceAuthenticationHandler : DelegatingHandler
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServiceAuthenticationHandler> _logger;
    private string _cachedToken;
    private DateTime _tokenExpiry;

    public ServiceAuthenticationHandler(
        IConfiguration configuration,
        ILogger<ServiceAuthenticationHandler> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_cachedToken) || DateTime.UtcNow >= _tokenExpiry)
            {
                _cachedToken = GenerateServiceToken();
                _tokenExpiry = DateTime.UtcNow.AddHours(1);
                _logger.LogInformation("Generated new service token");
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _cachedToken);

            var response = await base.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "Request failed: Status: {Status}, Content: {Content}",
                    response.StatusCode,
                    content);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in service authentication handler");
            throw;
        }
    }

    private string GenerateServiceToken()
    {
        var securityKey = _configuration["ServiceAuth:SecurityKey"];
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(securityKey));
        key.KeyId = "ServiceKeyId"; // Match KeyId used in validation

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "order_service"),
            new Claim("scope", "driver_service"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["ServiceAuth:Issuer"],
            audience: _configuration["ServiceAuth:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}