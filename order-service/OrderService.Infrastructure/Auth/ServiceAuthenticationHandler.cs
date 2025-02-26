using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Security.Claims;
using System.Text.Encodings.Web;

namespace OrderService.Infrastructure.Auth;


public class ServiceAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string ServiceApiKeyHeader = "X-Service-ApiKey";
    private readonly IConfiguration _configuration;

    public ServiceAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        Microsoft.AspNetCore.Authentication.ISystemClock clock,
        IConfiguration configuration)
        : base(options, logger, encoder, clock)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            if (!Request.Headers.TryGetValue(ServiceApiKeyHeader, out var apiKeyHeaderValues))
            {
                return Task.FromResult(AuthenticateResult.Fail("API Key is missing"));
            }

            var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
            var validApiKey = _configuration["ServiceAuth:ApiKey"];

            if (string.IsNullOrEmpty(providedApiKey) || providedApiKey != validApiKey)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid API Key"));
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "NotificationService"),
                new Claim(ClaimTypes.Role, "Service")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
        catch (Exception ex)
        {
            return Task.FromResult(AuthenticateResult.Fail(ex.Message));
        }
    }
}