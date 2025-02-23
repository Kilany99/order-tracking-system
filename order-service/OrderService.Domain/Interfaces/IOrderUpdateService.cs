using OrderService.Domain.Entities;

namespace OrderService.Domain.Interfaces;

public interface IOrderUpdateService
{
    Task SendOrderStatusUpdate(Guid orderId, OrderStatus status, Guid? driverId = null);
}