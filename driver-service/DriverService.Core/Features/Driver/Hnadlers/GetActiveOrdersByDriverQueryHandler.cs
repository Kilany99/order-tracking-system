
using DriverService.Core.Dtos;
using DriverService.Core.Features.Driver.Queries;
using DriverService.Domain.Exceptions;
using DriverService.Domain.Interfaces;
using MediatR;

namespace DriverService.Core.Features.Driver.Handlers;


public class GetActiveOrdersByDriverQueryHandler : IRequestHandler<GetActiveOrdersByDriverQuery, DriverOrderResponse>
{
    private readonly IDriverRepository _repository;

    public GetActiveOrdersByDriverQueryHandler(IDriverRepository repository)
    {
        _repository = repository;
    }

    public async Task<DriverOrderResponse> Handle(GetActiveOrdersByDriverQuery request, CancellationToken ct)
    {
        var driver = await _repository.GetByIdAsync(request.DriverId);
        if (driver.CurrentOrderId == null)
            throw new DriverNotFoundException(request.DriverId);
        return new DriverOrderResponse(
            driver.CurrentOrderId.Value,
            "",
            driver.CurrentLatitude,
            driver.CurrentLongitude,
            driver.Status.ToString(),
            DateTime.UtcNow
        );
    }
}