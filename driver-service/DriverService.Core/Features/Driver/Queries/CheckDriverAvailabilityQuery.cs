
using MediatR;

namespace DriverService.Core.Features.Driver.Queries;


public record CheckDriverAvailabilityQuery(Guid DriverId) : IRequest<bool>;
