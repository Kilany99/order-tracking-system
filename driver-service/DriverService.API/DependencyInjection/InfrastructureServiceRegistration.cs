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

            return services;
        }

    }
}
