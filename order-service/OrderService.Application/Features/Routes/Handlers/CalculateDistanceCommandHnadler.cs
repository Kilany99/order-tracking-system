using MediatR;
using OrderService.Application.Features.Routes.Commands;
using OrderService.Infrastructure.Routing.Interfaces;

namespace OrderService.Application.Features.Routes.Handlers;

public class CalculateDistanceCommandHandler : IRequestHandler<CalculateDistanceCommand, double>
{
    private readonly IRoutingService _routingService;
    public CalculateDistanceCommandHandler(IRoutingService routingService) => _routingService = routingService;

    public async Task<double> Handle(CalculateDistanceCommand command, CancellationToken cancellationToken) =>
        await _routingService.CalculateDistance(command.startLat, command.startLng, command.endLat, command.endLng);

}

