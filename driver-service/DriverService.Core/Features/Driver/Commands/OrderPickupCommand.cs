

using MediatR;

namespace DriverService.Core.Features.Driver.Commands;

public record OrderPickupCommand(Guid OrderId,
    Guid DriverId,
    string DriverName) : IRequest<Unit>;