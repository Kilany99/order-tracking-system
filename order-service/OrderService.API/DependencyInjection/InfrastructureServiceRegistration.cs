using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.Features.Orders.Commands;
using OrderService.Application.Features.Orders.Handlers;
using OrderService.Application.Features.Orders.Queries;
using OrderService.Application.Responses;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Repositories;
using System.Reflection;

namespace OrderService.API.DependencyInjection;


public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<OrderDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("Postgres")));

        services.AddScoped<IOrderRepository, OrderRepository>();
        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        
        services.AddScoped<IRequestHandler<GetOrderByIdQuery, OrderResponse>, GetOrderByIdQueryHandler>();
        services.AddScoped<IRequestHandler<UpdateOrderStatusCommand, Guid>, UpdateOrderStatusCommandHandler>();
        services.AddScoped<IRequestHandler<UpdateOrderCommand, ApiResponse<Guid>>, UpdateOrderCommandHandler>();
        services.AddScoped<IRequestHandler<GetAllOrdersQuery, List<OrderResponse>>, GetAllOrdersQueryHandler>();
        services.AddScoped<IRequestHandler<DeleteOrderCommand, bool>, DeleteOrderCommandHandler>();

        return services;
    }
}