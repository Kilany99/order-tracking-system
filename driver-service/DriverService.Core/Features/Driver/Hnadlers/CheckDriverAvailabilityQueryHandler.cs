using DriverService.Core.Features.Driver.Queries;
using DriverService.Domain.Interfaces;
using MediatR;

namespace DriverService.Core.Features.Driver.Handlers;


public class CheckDriverAvailabilityQueryHandler : IRequestHandler<CheckDriverAvailabilityQuery, bool>
{
    private readonly IDriverRepository _repository;

    public CheckDriverAvailabilityQueryHandler(IDriverRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(CheckDriverAvailabilityQuery request, CancellationToken ct)
    {
        var driver = await _repository.GetByIdAsync(request.DriverId);
        return driver?.IsAvailable ?? false;
    }
}