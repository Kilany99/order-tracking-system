using DriverService.Core.Features.Driver.Queries;
using DriverService.Domain.Interfaces;
using DriverService.Infrastructure.Repositories;
using MediatR;

namespace DriverService.Core.Features.Driver.Hnadlers;

public class FindNearestDriverQueryHandler : IRequestHandler<FindNearestDriverQuery, Guid>
{
    private readonly IDriverAssignmentService _driverAssignmentService;

    public FindNearestDriverQueryHandler(IDriverAssignmentService driverAssignmentService)
    {
        _driverAssignmentService = driverAssignmentService;
        
    }

    public async Task<Guid> Handle(FindNearestDriverQuery request, CancellationToken cancellationToken)=>
          await _driverAssignmentService.FindNearestDriver(request.Latitude, request.Longitude);
    
}
