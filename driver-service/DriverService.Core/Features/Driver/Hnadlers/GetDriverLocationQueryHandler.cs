using DriverService.Core.Dtos;
using DriverService.Core.Features.Driver.Queries;
using DriverService.Domain.Exceptions;
using DriverService.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverService.Core.Features.Driver.Hnadlers;


public class GetDriverLocationQueryHandler
    : IRequestHandler<GetDriverLocationQuery, DriverLocationResponse>
{
    private readonly IDriverRepository _repository;

    public GetDriverLocationQueryHandler(IDriverRepository repository)
    {
        _repository = repository;
    }

    public async Task<DriverLocationResponse> Handle(
        GetDriverLocationQuery request,
        CancellationToken cancellationToken)
    {
        var driver = await _repository.GetByIdAsync(request.DriverId)
            ?? throw new DriverNotFoundException(request.DriverId);

        return new DriverLocationResponse(
            driver.Id,
            driver.CurrentLatitude,
            driver.CurrentLongitude,
            driver.UpdatedAt); 
    }
}