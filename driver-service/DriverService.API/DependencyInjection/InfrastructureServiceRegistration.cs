using DriverService.Domain.Interfaces;
using DriverService.Infrastructure.Persistence;
using DriverService.Infrastructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using FluentValidation;
using DriverService.Core.Features.Driver.Commands;
using DriverService.Core.Dtos;
using DriverService.Core.Features.Driver.Hnadlers;
using DriverService.Core.Features.Driver.Queries;
using DriverService.Core.QueryHandlers;
using DriverService.Core.Features.Driver.Handlers;
using DriverService.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using DriverService.Domain.Entities;
using System.Security.Claims;
namespace DriverService.API.DependencyInjection
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            services.AddDbContext<DriverDbContext>(options =>
                options.UseNpgsql(config.GetConnectionString("Postgres")));
            services.AddScoped<MongoDbContext>(sp =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                return new MongoDbContext(config);
            });
            services.AddScoped<IDriverAssignmentService, DriverAssignmentService>();
            services.AddScoped<IDriverRepository, DriverRepository>();
            return services;
        }

        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            services.AddScoped<IRequestHandler<RegisterDriverCommand, DriverResponse>, AuthRegisterDriverCommandHandler>();
            services.AddScoped<IRequestHandler<LoginDriverCommand, AuthResponse>, LoginDriverCommandHandler>();

            services.AddScoped<IRequestHandler<UpdateDriverLocationCommand, Unit>, UpdateDriverLocationCommandHandler>();
            services.AddScoped<IRequestHandler<GetDriverLocationQuery, DriverLocationResponse>, GetDriverLocationQueryHandler>();

            services.AddScoped<IRequestHandler<GetAvailableDriversQuery, IEnumerable<DriverResponse>>, GetAvailableDriversQueryHandler>();
            services.AddScoped<IRequestHandler<GetDriverByIdQuery, DriverResponse>, GetDriverByIdQueryHandler>();
            services.AddScoped<IRequestHandler<DeleteDriverCommand, bool>, DeleteDriverCommandHandler>();
            services.AddScoped<IRequestHandler<GetDriverLocationQuery, DriverLocationResponse>, GetDriverLocationQueryHandler>();

            services.AddScoped<IRequestHandler<AssignDriverCommand, Guid>, AssignDriverCommandHandler>();
            services.AddScoped<IRequestHandler<FindNearestDriverQuery, Guid>, FindNearestDriverQueryHandler>();

            services.AddScoped<IRequestHandler<GetActiveOrdersByDriverQuery, DriverOrderResponse>, GetActiveOrdersByDriverQueryHandler>();
            services.AddScoped<IRequestHandler<CheckDriverAvailabilityQuery, bool>, CheckDriverAvailabilityQueryHandler>();
            services.AddScoped<IRequestHandler<GetDriverByOrderIdQuery,DriverResponse>,GetDriverByOrderIdQueryHandler>();

            return services;
        }

        public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure keys with KeyIds
            var defaultKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
            defaultKey.KeyId = "DefaultKeyId"; // Match token generation

            var serviceKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["ServiceAuth:SecurityKey"]));
            serviceKey.KeyId = "ServiceKeyId"; // Unique ID for service tokens

            services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"], 
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Key"])),
                    RoleClaimType = "role",

                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>();

                        // Log all claims
                        var claims = context.Principal.Claims.Select(c => $"{c.Type}: {c.Value}");
                        logger.LogInformation("Token validated with claims: {Claims}",
                            string.Join(", ", claims));
                        // Check for role claim
                        var roleClaim = context.Principal.Claims
                            .FirstOrDefault(c => c.Type == "role");
                        logger.LogInformation("Role claim found: {RoleClaim}",
                            roleClaim?.Value ?? "none");
                        // Log authentication result
                        logger.LogInformation("IsAuthenticated: {IsAuthenticated}",
                            context.Principal.Identity.IsAuthenticated);
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                     .GetRequiredService<ILogger<Program>>();
                        logger.LogError(context.Exception,
                            "Authentication failed: {ErrorMessage}",
                            context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();
                        logger.LogWarning("Authorization challenge issued: {Error}",
                            context.Error);
                        return Task.CompletedTask;
                    
                    }
                };
            
            })
            .AddJwtBearer("ServiceAuth", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["ServiceAuth:Issuer"],
                    ValidAudience = configuration["ServiceAuth:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["ServiceAuth:SecurityKey"]))
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();
                        logger.LogInformation("Service token validated successfully");
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILogger<Program>>();
                        logger.LogError(context.Exception, "Service authentication failed");
                        return Task.CompletedTask;
                    }
                };
            });

            // Add authorization policies
            services.AddAuthorization(options =>
            {

                // Policy for driver endpoints
                options.AddPolicy("DriverPolicy", policy =>
                  policy.AddAuthenticationSchemes("Bearer")
                      .RequireClaim(ClaimTypes.Role, "driver"));

                // Policy for service-to-service communication
                options.AddPolicy("ServicePolicy", policy =>
                    policy.AddAuthenticationSchemes("ServiceAuth")
                        .RequireClaim("scope", "driver_service"));

                // Combined policy
                options.AddPolicy("DriverOrServicePolicy", policy =>
                {
                    policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, "ServiceAuth")
                          .RequireAssertion(context =>
                          {
                              var hasDriverRole = context.User.HasClaim("role", "driver");
                              var hasServiceScope = context.User.HasClaim("scope", "driver_service");
                              return hasDriverRole || hasServiceScope;
                          });
                });
            });
            return services;

        }

    }
}
