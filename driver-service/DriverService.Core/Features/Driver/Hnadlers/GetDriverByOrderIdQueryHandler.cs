
using DriverService.Core.Dtos;
using DriverService.Core.Features.Driver.Queries;
using DriverService.Domain.Entities;
using DriverService.Domain.Interfaces;
using MediatR;

namespace DriverService.Core.Features.Driver.Hnadlers;

public class GetDriverByOrderIdQueryHandler : IRequestHandler<GetDriverByOrderIdQuery, DriverResponse>
{
    private readonly IDriverRepository _repository;

    public GetDriverByOrderIdQueryHandler(IDriverRepository driverRepository)
    {
        _repository = driverRepository;
    }

    public async Task<DriverResponse> Handle(GetDriverByOrderIdQuery request,CancellationToken cancellationToken)=> await _repository.GetByOrderId(request.OrderID);
    
}
