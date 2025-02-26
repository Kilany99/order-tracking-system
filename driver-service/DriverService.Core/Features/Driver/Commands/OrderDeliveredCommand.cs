
using MediatR;

namespace DriverService.Core.Features.Driver.Commands;

public record OrderDeliveredCommand(
    Guid DriverId,
    Guid OrderId) : IRequest<Unit>;