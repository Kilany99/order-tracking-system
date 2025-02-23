using DriverService.Domain.Entities;
using DriverService.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DriverService.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;

    public JwtService(IConfiguration config)
    {
        _config = config;
    }

    public string GenerateToken(Driver driver,DriverAuth auth)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
        securityKey.KeyId = "DefaultKeyId";
        var credentials = new SigningCredentials(
            securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, driver.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, auth.Email),
            new Claim("vehicleType", driver.VehicleType),
            new Claim(ClaimTypes.Role, "driver") 
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims, 
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials
        );
       
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    public RefreshToken GenerateRefreshToken()
    {
        return new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.UtcNow.AddDays(
                Convert.ToDouble(_config["Jwt:RefreshTokenExpiryDays"]))
        };
    }
}