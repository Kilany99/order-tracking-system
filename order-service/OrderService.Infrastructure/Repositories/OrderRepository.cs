using Microsoft.EntityFrameworkCore;
using OrderService.Infrastructure.Helpers;
using OrderService.Infrastructure.Data;
using OrderService.Domain.Entities;
namespace OrderService.Infrastructure.Repositories;


public interface IOrderRepository
{
    Task AddAsync(Order order);
    Task<Order> GetByIdAsync(Guid id);
    Task<List<Order>> GetAllAsync();

    Task UpdateAsync(Order order);
    Task DeleteAsync(Order order);
    Task<List<Order>> GetOrdersNearLocationAsync(double latitude,double longitude, double radiusInMeters);
    Task<bool> TryAssignDriver(Guid orderId, Guid driverId);


}
public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;

    public OrderRepository(OrderDbContext context) => _context = context;

    public async Task AddAsync(Order order)
    {
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync(); 
    }
    public async Task<Order> GetByIdAsync(Guid id) => await _context.Orders.FindAsync(id);

    public async Task<List<Order>> GetAllAsync() => await Task.FromResult(_context.Orders.ToList());

    public async Task UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
    }


    public async Task DeleteAsync(Order order)
    {
        _context.Orders.Remove(order);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Order>> GetOrdersNearLocationAsync(
    double latitude,
    double longitude,
    double radiusInMeters)
    {
        var orders = await _context.Orders
            .Where(o => o.Status == OrderStatus.Created)
            .ToListAsync();

        return orders
            .Where(o => GeoHelper.CalculateDistance(
                latitude, longitude,
                o.DeliveryLatitude, o.DeliveryLongitude) <= radiusInMeters)
            .ToList();
    }
    public async Task<bool> TryAssignDriver(Guid orderId, Guid driverId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.Status == OrderStatus.Created);

            if (order == null) return false;

            order.DriverId = driverId;
            order.UpdateStatus(OrderStatus.Preparing);
            order.AssignedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

}