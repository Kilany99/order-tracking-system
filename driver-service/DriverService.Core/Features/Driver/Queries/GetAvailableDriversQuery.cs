

using DriverService.Core.Dtos;
using MediatR;

namespace DriverService.Core.Features.Driver.Queries;

public record GetAvailableDriversQuery : IRequest<IEnumerable<DriverResponse>>;

