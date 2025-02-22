using DriverService.Core.Dtos;
using DriverService.Domain.Entities;
using MediatR;

namespace DriverService.Core.Features.Driver.Queries;

public record GetDriverByIdQuery(Guid Id) : IRequest<DriverResponse>;

