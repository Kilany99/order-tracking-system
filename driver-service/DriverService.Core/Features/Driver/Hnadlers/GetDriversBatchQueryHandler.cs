using AutoMapper;
using DriverService.Core.Features.Driver.Queries;
using DriverService.Domain.Interfaces;
using DriverService.Domain.Models;
using MediatR;

namespace DriverService.Core.Features.Driver.Hnadlers;


public class GetDriversBatchQueryHandler : IRequestHandler<GetDriversBatchQuery, List<DriverDetailsDto>>
{
    private readonly IDriverRepository _driverRepository;
    private readonly IMapper _mapper;

    public GetDriversBatchQueryHandler(IDriverRepository driverRepository, IMapper mapper)
    {
        _driverRepository = driverRepository;
        _mapper = mapper;
    }

    public async Task<List<DriverDetailsDto>> Handle(GetDriversBatchQuery request, CancellationToken cancellationToken)
    {
        var drivers = await _driverRepository.GetDriversByIdsAsync(request.DriverIds);
        return _mapper.Map<List<DriverDetailsDto>>(drivers);
    }
}
