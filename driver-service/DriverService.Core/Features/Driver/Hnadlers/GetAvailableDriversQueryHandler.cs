using DriverService.Core.Dtos;
using DriverService.Core.Features.Driver.Queries;
using DriverService.Domain.Entities;
using DriverService.Domain.Interfaces;
using MediatR;

namespace DriverService.Core.QueryHandlers
{
    public class GetAvailableDriversQueryHandler
        : IRequestHandler<GetAvailableDriversQuery, IEnumerable<DriverResponse>>
    {
        private readonly IDriverRepository _repository;

        public GetAvailableDriversQueryHandler(IDriverRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<DriverResponse>> Handle(
            GetAvailableDriversQuery request,
            CancellationToken cancellationToken)
        {
            var drivers = await _repository.GetAvailableDriversAsync();
            return drivers.Select(MapToDriverResponse);
        }

        private static DriverResponse MapToDriverResponse(Driver driver)
        {
            return new DriverResponse(
                driver.Id,
                driver.Name,
                driver.VehicleType,
                driver.IsAvailable);
        }
    }
}