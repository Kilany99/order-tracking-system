namespace OrderService.Domain.Models;

public class DriverResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string VehicleType { get; set; }
    public bool IsAvailable { get; set; } = true;

    public DriverResponse() { }

    public DriverResponse(Guid id, string name, string vechicleType, bool isAvailable)
    {
        Id = id; Name = name; VehicleType = vechicleType; IsAvailable = isAvailable;
    }
}