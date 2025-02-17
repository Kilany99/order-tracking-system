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


    // Navigation properties
    public DriverAuth Auth { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; }
}