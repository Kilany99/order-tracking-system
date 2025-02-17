using MediatR;
using DriverService.Core.Features.Driver.Commands;
using DriverService.Domain.Interfaces;
using DriverService.Core.Features.Driver.Hnadlers;
using Microsoft.Extensions.Logging;
using DriverService.Domain.Exceptions;

namespace DriverService.Core.Features.Driver.Handlers
{
    public class AssignDriverCommandHandler : IRequestHandler<AssignDriverCommand, Guid>
    {
        private readonly IDriverAssignmentService _driverAssignmentService;
        private readonly ILogger<AssignDriverCommandHandler> _logger;

        public AssignDriverCommandHandler(IDriverAssignmentService driverAssignmentService, ILogger<AssignDriverCommandHandler> logger)
        {
            _driverAssignmentService = driverAssignmentService;
            _logger = logger;
        }

        public async Task<Guid> Handle(AssignDriverCommand request, CancellationToken cancellationToken)
        {
            var driverId = await _driverAssignmentService.FindNearestDriver(request.Longitude, request.Latitude);
            return await _driverAssignmentService.UpdateDriverPosition(driverId,request.Longitude, request.Longitude);
        }

        
    
    }
}
