// Shared DTOs for service-service communications

using OrderService.Domain.Entities;

namespace OrderService.Domain.Models;
public record OrderDetailsDto
{
    public Guid Id { get; init; }
    public string CustomerId { get; init; }
    public string DeliveryAddress { get; init; }
    public OrderStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public Guid? DriverId { get; init; }
    public DriverDetailsDto? Driver { get; init; }
}

public record DriverDetailsDto
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string PhoneNumber { get; init; }
    public double? CurrentLatitude { get; init; }
    public double? CurrentLongitude { get; init; }
}