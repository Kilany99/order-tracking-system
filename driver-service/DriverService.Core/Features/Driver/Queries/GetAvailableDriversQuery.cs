

using DriverService.Core.Dtos;
using DriverService.Domain.Entities;
using MediatR;

namespace DriverService.Core.Features.Driver.Queries;

public record GetAvailableDriversQuery : IRequest<IEnumerable<DriverResponse>>;

