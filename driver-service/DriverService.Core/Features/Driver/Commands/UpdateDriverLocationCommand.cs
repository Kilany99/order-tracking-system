

using MediatR;

namespace DriverService.Core.Features.Driver.Commands;

public record UpdateDriverLocationCommand(
    Guid DriverId,
    double Latitude,
    double Longitude) : IRequest<Unit>;
