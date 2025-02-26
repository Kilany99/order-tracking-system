using NotificationService.Domain.Events;

namespace NotificationService.Domain.Interfaces;


public interface INotificationHandler
{
    Task HandleOrderNotificationAsync(OrderNotificationEvent notification);
}