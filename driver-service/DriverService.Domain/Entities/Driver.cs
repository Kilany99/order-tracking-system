using DriverService.Domain.Exceptions;
using NpgsqlTypes;

namespace DriverService.Domain.Entities;

public class Driver
{
    public Guid Id { get; set; }
    public Guid? CurrentOrderId { get; set; }

    public string Name { get; set; }
    public string VehicleType { get; set; }
    public bool IsAvailable { get; set; }
    public DateTime RegistrationDate { get; set; }
    public double CurrentLatitude { get; set; }
    public double CurrentLongitude { get; set; }

    public DateTime UpdatedAt { get; set; }
    public DriverStatus Status { get; private set; } = DriverStatus.Offline;
    public DateTime? LastAvailableTime { get; private set; }


    // Navigation properties
    public DriverAuth Auth { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; }

    public void GoOffline() => Status = DriverStatus.Offline;
    public void AcceptOrder(Guid orderId)
    {
        if (Status != DriverStatus.Available)
            throw new DriverDomainException("Driver not available");

        Status = DriverStatus.Assigned;
        CurrentOrderId = orderId;
    }

    public void StartDelivery()
    {
        if (Status != DriverStatus.Assigned)
            throw new InvalidOperationException("Driver must be assigned first");

        Status = DriverStatus.InDelivery;
        LastAvailableTime = DateTime.UtcNow;
    }

    public void CompleteDelivery()
    {
        if (Status != DriverStatus.InDelivery)
            throw new InvalidOperationException("Driver must be on delivery");

        CurrentOrderId = null;
        Status = DriverStatus.Available;
        LastAvailableTime = DateTime.UtcNow;
    }
}

public enum DriverStatus { Offline, Available, Assigned, InDelivery }
