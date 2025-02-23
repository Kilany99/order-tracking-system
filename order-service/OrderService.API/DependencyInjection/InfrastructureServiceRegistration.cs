using FluentValidation;
using MediatR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.EntityFrameworkCore;
using OrderService.API.Clients;
using OrderService.API.Handlers;
using OrderService.Application.Features.Orders.Commands;
using OrderService.Application.Features.Orders.Handlers;
using OrderService.Application.Features.Orders.Queries;
using OrderService.Application.Features.Routes.Commands;
using OrderService.Application.Features.Routes.Handlers;
using OrderService.Application.Features.Routes.Queries;
using OrderService.Application.Responses;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Repositories;
using OrderService.Infrastructure.Routing.Interfaces;
using OrderService.Infrastructure.Routing.Models;
using OrderService.Infrastructure.Routing.Services;
using OrderService.Infrastructure.Services;
using System.Net.Http.Headers;
using System.Reflection;

namespace OrderService.API.DependencyInjection;


public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<OrderDbContext>(options =>
            options.UseNpgsql(config.GetConnectionString("Postgres")));

        services.AddScoped<IOrderRepository, OrderRepository>();


        // Add Connection Manager
        services.AddSingleton<ICustomConnectionManager, CustomConnectionManager>();
        // Register HTTP Client
        services.AddHttpClient<IDriverClient, HttpDriverClient>(client =>
        {
            client.BaseAddress = new Uri(config["DriverService:BaseUrl"]);
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddHttpMessageHandler<ServiceAuthenticationHandler>();
        services.AddTransient<ServiceAuthenticationHandler>();


        services.AddHttpClient<IRoutingService, OSRMRoutingService>();
        services.AddMemoryCache();
        services.AddScoped<IRoutingService, OSRMRoutingService>();

        return services;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IRequestHandler<CreateOrderCommand, ApiResponse<Guid>>, CreateOrderCommandHandler>();
        services.AddScoped<IRequestHandler<GetOrderByIdQuery, OrderResponse>, GetOrderByIdQueryHandler>();
        services.AddScoped<IRequestHandler<UpdateOrderStatusCommand, Guid>, UpdateOrderStatusCommandHandler>();
        services.AddScoped<IRequestHandler<UpdateOrderCommand, ApiResponse<Guid>>, UpdateOrderCommandHandler>();
        services.AddScoped<IRequestHandler<GetAllOrdersQuery, List<OrderResponse>>, GetAllOrdersQueryHandler>();
        services.AddScoped<IRequestHandler<DeleteOrderCommand, bool>, DeleteOrderCommandHandler>();

        //routes commands and queries 
        services.AddScoped<IRequestHandler<GetRouteQuery,RouteResponse>,GetRouteQueryHandler>();
        services.AddScoped<IRequestHandler<CalculateETACommand, TimeSpan>, CalculateETACommandHandler>();
        services.AddScoped<IRequestHandler<CalculateDistanceCommand, double>, CalculateDistanceCommandHandler>();



        return services;
    }
}