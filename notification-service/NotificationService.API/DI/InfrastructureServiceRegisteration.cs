using NotificationService.Domain.Events;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Channels;
using NotificationService.Infrastructure.NotificationsMetrics;
using NotificationService.Infrastructure.Serialization;
using NotificationService.Infrastructure.Services;

namespace NotificationService.API.DI;

public static class InfrastructureServiceRegisteration
{

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddSingleton<OrderEventDeserializer>();
        services.AddHttpClient<ICustomerService, CustomerService>();
        services.AddScoped<IEmailService, GmailService>();
        services.AddSingleton<NotificationMetrics>();  


        // Register channels
        services.AddSingleton<INotificationChannel<OrderNotificationEvent>, NotificationChannel<OrderNotificationEvent>>();
        services.AddSingleton<INotificationChannel<OrderNotificationEvent>, NotificationChannel<OrderNotificationEvent>>();
        services.AddSingleton<INotificationChannel<OrderNotificationEvent>, NotificationChannel<OrderNotificationEvent>>();
        services.AddSingleton<INotificationChannel<OrderNotificationEvent>, NotificationChannel<OrderNotificationEvent>>();

        return services;
    }

}
