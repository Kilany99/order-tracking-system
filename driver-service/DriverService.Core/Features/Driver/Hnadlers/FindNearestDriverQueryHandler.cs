using DriverService.Core.Features.Driver.Queries;
using DriverService.Domain.Exceptions;
using DriverService.Domain.Interfaces;
using DriverService.Infrastructure.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DriverService.Core.Features.Driver.Hnadlers;

public class FindNearestDriverQueryHandler : IRequestHandler<FindNearestDriverQuery, Guid>
{
    private readonly IDriverRepository _driverRepository;
    private readonly ILogger<FindNearestDriverQueryHandler> _logger;

    public FindNearestDriverQueryHandler(IDriverRepository driverRepository, ILogger<FindNearestDriverQueryHandler> logger)
    {
        _driverRepository = driverRepository;
        _logger = logger;
        
    }

    public async Task<Guid> Handle(FindNearestDriverQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Trying to find nearest driver for the given coordenates: Long {request.Longitude} , Lat {request.Latitude} ");
        var driver = await _driverRepository.FindNearestDriverAsync(
               request.Latitude,
               request.Longitude) ?? throw new DriverNotFoundException("No Drivers Found!");
        return driver.Id;

    }
    
}
