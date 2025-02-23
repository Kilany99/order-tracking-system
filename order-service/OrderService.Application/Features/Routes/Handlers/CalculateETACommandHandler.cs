using MediatR;
using OrderService.Application.Features.Routes.Commands;
using OrderService.Infrastructure.Routing.Interfaces;

namespace OrderService.Application.Features.Routes.Handlers;
public class CalculateETACommandHandler : IRequestHandler<CalculateETACommand,TimeSpan>
{
    private readonly IRoutingService _routingService;
    public CalculateETACommandHandler(IRoutingService routingService) => _routingService = routingService;

    public async Task<TimeSpan> Handle(CalculateETACommand command,CancellationToken cancellationToken) =>
        await _routingService.CalculateETA(command.startLat, command.startLng, command.endLat, command.endLng);
    
}
