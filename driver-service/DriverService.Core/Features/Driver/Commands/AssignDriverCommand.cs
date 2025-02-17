using MediatR;

namespace DriverService.Core.Features.Driver.Commands;

public record AssignDriverCommand(double Latitude, double Longitude,Guid OrderId) : IRequest<Guid>;

