using DriverService.Core.Dtos;
using MediatR;

namespace DriverService.Core.Features.Driver.Queries;

public record GetDriverByIdQuery(Guid Id) : IRequest<DriverResponse>;

