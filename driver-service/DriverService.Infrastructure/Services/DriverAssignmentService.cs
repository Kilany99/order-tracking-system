using DriverService.Domain.Exceptions;
using DriverService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace DriverService.Infrastructure.Services;

public class DriverAssignmentService : IDriverAssignmentService
{
    private readonly IDriverRepository _repository;
    private readonly ILogger<DriverAssignmentService> _logger;

    public DriverAssignmentService(
        IDriverRepository repository,
        ILogger<DriverAssignmentService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<Guid> FindNearestDriver(double lat, double lon)
    {
        var drivers = await _repository.GetAvailableDriversAsync();

        if (!drivers.Any())
            throw new NoAvailableDriversException();

        var nearestDriver = drivers
            .AsParallel()
            .Select(d => new {
                Driver = d,
                Distance = CalculateDistance(d.CurrentLatitude, d.CurrentLongitude, lat, lon)
            })
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        if (nearestDriver == null)
            throw new NoAvailableDriversException();

        _logger.LogInformation("Selected driver {DriverId} at {Distance}m",
            nearestDriver.Driver.Id, nearestDriver.Distance);

        return nearestDriver.Driver.Id;
    }

    private double CalculateDistance(
       double lat1, double lon1,
       double lat2, double lon2
   )
    {
        const double R = 6371e3; // Earth radius in meters
        var φ1 = lat1 * Math.PI / 180;
        var φ2 = lat2 * Math.PI / 180;
        var Δφ = (lat2 - lat1) * Math.PI / 180;
        var Δλ = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                Math.Cos(φ1) * Math.Cos(φ2) *
                Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }
    public async Task<Guid> UpdateDriverPosition(Guid driverId, double lat, double lon)
    {
        var driver = await _repository.GetByIdAsync(driverId)??throw new DriveNotFoundException();
        driver.CurrentLatitude = lat;
        driver.CurrentLongitude = lon;
        driver.IsAvailable = false;
        await _repository.UpdateAsync(driver);
        return driver.Id;
    }
}