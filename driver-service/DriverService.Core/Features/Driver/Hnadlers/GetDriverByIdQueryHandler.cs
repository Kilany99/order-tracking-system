using DriverService.Core.Dtos;
using DriverService.Core.Features.Driver.Queries;
using DriverService.Domain.Entities;
using DriverService.Domain.Exceptions;
using DriverService.Domain.Interfaces;
using MediatR;


namespace DriverService.Core.Features.Driver.Handlers;

public class GetDriverByIdQueryHandler : IRequestHandler<GetDriverByIdQuery, DriverResponse>
{
    private readonly IDriverRepository _driverRepository;

    public GetDriverByIdQueryHandler(IDriverRepository driverRepository)
    {
        _driverRepository = driverRepository;
    }

    public async Task<DriverResponse> Handle(GetDriverByIdQuery request, CancellationToken cancellationToken)
    {
        var driver = await _driverRepository.GetByIdAsync(request.Id) ?? throw new DriverNotFoundException("Driver not found!");
        var driverResponse = new DriverResponse(driver.Id, driver.VehicleType, driver.Name, driver.IsAvailable);
        if(driverResponse.CurrentOrderId != null)
            driverResponse.SetOrderId(driver.CurrentOrderId.Value);
        return driverResponse;
    }
}
