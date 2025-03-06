using OrderService.Domain.Exceptions;
using OrderService.Domain.Models;

namespace OrderService.Domain.Entities;
public class Order : Entity<Guid>
{
    public string? CustomerId { get; private set; }
    public Guid? DriverId { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? DeliveryAddress { get; private set; }
    public OrderStatus Status { get; private set; }
    public double DeliveryLatitude { get; set; }
    public double DeliveryLongitude { get; set; }
    public DateTime? AssignedAt { get; set; }

    public int AssignmentRetryCount { get; set; }
    public DateTime? LastAssignmentAttempt { get; set; }
    public DateTime? NextAssignmentAttempt { get; set; }

    public DriverDetails? DriverDetails { get; private set; }

 


    // Factory method for controlled creation
    public static Order Create(
        string customerId,
        string deliveryAddress,
        double deliveryLatitude,
        double deliveryLongitude)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            DeliveryAddress = deliveryAddress,
            DeliveryLatitude = deliveryLatitude,
            DeliveryLongitude = deliveryLongitude,
            Status = OrderStatus.Created,
            CreatedAt = DateTime.UtcNow
        };
    }
    public void UpdateStatus(OrderStatus status) =>
        Status = status;
    public void UpdateCustomerId(string customerId)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new OrderDomainException("Customer ID cannot be empty");

        CustomerId = customerId;
    }

    public void UpdateDeliveryAddress(string deliveryAddress)
    {
        if (string.IsNullOrWhiteSpace(deliveryAddress))
            throw new OrderDomainException("Delivery address cannot be empty");

        DeliveryAddress = deliveryAddress;
    }

    public void MarkAsOutForDelivery() => Status = OrderStatus.OutForDelivery;

    public void MarkAsPreparing()
    {
        if (Status != OrderStatus.Created)
        {
            throw new InvalidOperationException(
                $"Cannot mark as preparing. Current status: {Status}");
        }

        if (!DriverId.HasValue)
        {
            throw new InvalidOperationException(
                "Cannot mark as preparing without assigned driver");
        }

        Status = OrderStatus.Preparing;
        AssignedAt = DateTime.UtcNow;
    }
    public void MarkAsCancelled() => Status = OrderStatus.Cancelled;
    public void MarkAsDelivered() => Status = OrderStatus.Delivered;
    public void SetDriverId(Guid driverId) => DriverId = driverId;

    public bool NeedsRetry(Order order, DateTime currentTime) => order.Status == OrderStatus.Created &&
               order.NextAssignmentAttempt.HasValue &&
               order.NextAssignmentAttempt.Value <= currentTime;

    public void SetDriverDetails(DriverDetails driverDetails)
    {
        DriverDetails = driverDetails;
    }
}

public enum OrderStatus { Created, Preparing, OutForDelivery, Delivered, Cancelled }