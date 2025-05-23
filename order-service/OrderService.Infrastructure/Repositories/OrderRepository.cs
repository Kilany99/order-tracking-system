﻿using Microsoft.EntityFrameworkCore;
using OrderService.Infrastructure.Helpers;
using OrderService.Infrastructure.Data;
using OrderService.Domain.Entities;
using Microsoft.Extensions.Logging;
using OrderService.Infrastructure.Clients;
using OrderService.Domain.Extensions;
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
    Task<IEnumerable<Order>> GetOrdersByDriver(Guid driverId);
    Task<bool> SaveChangesAsync();

    Task<IEnumerable<Order>> GetOrdersWithPendingAssignmentAsync();
    Task UpdateOrderAssignmentAttemptAsync(Guid orderId, int retryCount, DateTime nextAttemptTime);
    Task<(int RetryCount, DateTime LastAttemptTime)?> GetOrderAssignmentAttemptsAsync(Guid orderId);

    Task<List<Order>> GetOrdersByCustomerIdAsync(string customerId);
    Task<List<Order>> GetActiveOrdersByCustomerIdAsync(string customerId);


}



public class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _context;
    private readonly ILogger<OrderRepository> _logger;
    private readonly IDriverClient _driverClient;

    public OrderRepository(OrderDbContext context,ILogger<OrderRepository> logger, IDriverClient driverClient)
    {
        _context = context;
        _logger = logger;
        _driverClient = driverClient;
    }

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
    public async Task<IEnumerable<Order>> GetOrdersByDriver(Guid driverId)
    {
        try
        {
            return await _context.Orders
                .Where(o => o.DriverId == driverId &&
                           o.Status != OrderStatus.Delivered)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "//Order Repo//:Error getting orders for driver {DriverId}",
                driverId);
            return [];
        }
    }
    public async Task<IEnumerable<Order>> GetOrdersWithPendingAssignmentAsync()
    {
        try
        {
            return await _context.Orders
                .Where(o => o.Status == OrderStatus.Created &&
                           (o.LastAssignmentAttempt == null ||
                            o.NextAssignmentAttempt <= DateTime.UtcNow))
                .OrderBy(o => o.NextAssignmentAttempt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "//Order Repo//:Error retrieving orders with pending assignment");
            throw;
        }
    }

    public async Task UpdateOrderAssignmentAttemptAsync(
        Guid orderId,
        int retryCount,
        DateTime nextAttemptTime)
    {
        try
        {
            var order = await GetByIdAsync(orderId);
            if (order == null)
            {
                _logger.LogError($"Order {orderId} not found");
                return;
            }

            order.AssignmentRetryCount = retryCount;
            order.LastAssignmentAttempt = DateTime.UtcNow;
            order.NextAssignmentAttempt = nextAttemptTime;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "//Order Repo//:Updated assignment attempt for order {OrderId}. Retry count: {RetryCount}, Next attempt: {NextAttempt}",
                orderId,
                retryCount,
                nextAttemptTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "//Order Repo//:Error updating assignment attempt for order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<(int RetryCount, DateTime LastAttemptTime)?> GetOrderAssignmentAttemptsAsync(Guid orderId)
    {
        var order = await GetByIdAsync(orderId);
        if (order == null || order.LastAssignmentAttempt == null)
        {
            return null;
        }

        return (order.AssignmentRetryCount, order.LastAssignmentAttempt.Value);
    }
    public async Task<List<Order>> GetOrdersByCustomerIdAsync(string customerId)
    {
        var orders = await _context.Orders
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        // Get unique driver IDs
        var driverIds = orders
            .Where(o => o.DriverId.HasValue)
            .Select(o => o.DriverId.Value)
            .Distinct();

        // Fetch driver details in batch
        var drivers = await _driverClient.GetDriversForOrdersAsync(driverIds);
        var driverMap = drivers.ToDictionary(d => d.Id);

        // Enrich orders with driver details
        foreach (var order in orders)
        {
            if (order.DriverId.HasValue && driverMap.TryGetValue(order.DriverId.Value, out var driver))
            {
                order.EnrichWithDriverDetails(driver);
            }
        }

        return orders;
    }

    public async Task<List<Order>> GetActiveOrdersByCustomerIdAsync(string customerId)
    {
        var activeStatuses = new[] {
            OrderStatus.Created,
            OrderStatus.Preparing,
            OrderStatus.OutForDelivery
        };

        var orders = await _context.Orders
            .Where(o => o.CustomerId == customerId && activeStatuses.Contains(o.Status))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        

        var driverIds = orders
            .Where(o => o.DriverId.HasValue)
            .Select(o => o.DriverId.Value)
            .Distinct();

        var drivers = await _driverClient.GetDriversForOrdersAsync(driverIds);
        var driverMap = drivers.ToDictionary(d => d.Id);

        foreach (var order in orders)
        {
            if (order.DriverId.HasValue && driverMap.TryGetValue(order.DriverId.Value, out var driver))
            {
                order.EnrichWithDriverDetails(driver);
            }
        }

        return orders;
    }
    public async Task<bool> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync() >= 0;
    }

}