
namespace DriverService.Domain.Interfaces;

public interface IDriverAssignmentService
{
    Task<Guid> FindNearestDriver(double lat, double lon);
    Task<Guid> UpdateDriverPosition(Guid driverId, double lat, double lon);
  
}
