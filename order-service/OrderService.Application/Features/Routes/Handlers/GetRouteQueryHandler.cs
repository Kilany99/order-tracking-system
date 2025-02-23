using MediatR;
using OrderService.Application.Features.Routes.Queries;
using OrderService.Infrastructure.Routing.Interfaces;
using OrderService.Infrastructure.Routing.Models;

namespace OrderService.Application.Features.Routes.Handlers;

public class GetRouteQueryHandler : IRequestHandler<GetRouteQuery, RouteResponse>
{
    private readonly IRoutingService _routingService;
    public GetRouteQueryHandler(IRoutingService routingService) => _routingService = routingService;
    public async Task<RouteResponse> Handle(GetRouteQuery request,CancellationToken cancellationToken) =>
         await _routingService.GetRoute(request.startLat,request.startLng,request.endLat,request.endLng);
}
