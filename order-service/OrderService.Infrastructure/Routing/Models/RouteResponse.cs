
namespace OrderService.Infrastructure.Routing.Models;

public class RouteResponse
{
    public double Distance { get; set; } // in meters
    public double Duration { get; set; } // in seconds
    public List<Coordinate> RoutePoints { get; set; }
    public DateTime EstimatedArrival { get; set; }
}
public struct Coordinate
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}