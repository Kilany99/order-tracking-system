
using DriverService.Core.Dtos;
using MediatR;

namespace DriverService.Core.Features.Driver.Queries;

public record GetActiveOrdersByDriverQuery(Guid DriverId) : IRequest<DriverOrderResponse>;