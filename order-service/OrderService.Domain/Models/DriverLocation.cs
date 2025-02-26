namespace OrderService.Domain.Models
{
    public record DriverLocation
    (Guid DriverId,
        double Latitude,
        double Longitude,
        DateTime Timestamp
    );
}