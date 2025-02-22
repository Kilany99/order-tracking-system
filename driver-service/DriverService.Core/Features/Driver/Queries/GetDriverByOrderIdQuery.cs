using DriverService.Domain.Entities;
using MediatR;


namespace DriverService.Core.Features.Driver.Queries;

public record GetDriverByOrderIdQuery (Guid OrderID) : IRequest<DriverResponse>;
