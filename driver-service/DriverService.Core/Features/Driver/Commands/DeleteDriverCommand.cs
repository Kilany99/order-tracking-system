using MediatR;


namespace DriverService.Core.Features.Driver.Commands;

public record DeleteDriverCommand(Guid DriverId) : IRequest<bool>;
