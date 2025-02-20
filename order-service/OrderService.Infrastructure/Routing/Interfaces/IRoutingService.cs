using OrderService.Infrastructure.Routing.Models;

namespace OrderService.Infrastructure.Routing.Interfaces;

public interface IRoutingService
{
    Task<TimeSpan> CalculateETA(double startLat, double startLng, double endLat, double endLng);
    Task<RouteResponse> GetRoute(double startLat, double startLng, double endLat, double endLng);
    Task<double> CalculateDistance(double startLat, double startLng, double endLat, double endLng);
}