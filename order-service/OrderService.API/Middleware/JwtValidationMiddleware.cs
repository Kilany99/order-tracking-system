using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace OrderService.API.Middleware;

public class JwtValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtValidationMiddleware> _logger;
    public JwtValidationMiddleware(RequestDelegate next,IConfiguration configuration,ILogger<JwtValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value;
        var anonymousPaths = new[] { "/auth/login", "/auth/register" };

        if (!anonymousPaths.Any(p => path.StartsWith(p)))
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

            if (string.IsNullOrEmpty(token) || !ValidateToken(token))
            {
                _logger.LogWarning("Not Authorized 401 !");
                context.Response.StatusCode = 401;
                return;
            }
        }

        await _next(context);
    }

    private bool ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        _logger.LogInformation("Validating token...");

        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidAudience = _configuration["JwtSettings:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:SecretKey"]))
            }, out _);
            _logger.LogInformation("Token validated successfully.");

            return true;
        }
        catch
        {
            _logger.LogInformation("Token validation failed.");
            return false;
        }
    }
}
