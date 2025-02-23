using MediatR;
using OrderService.Infrastructure.Routing.Models;

namespace OrderService.Application.Features.Routes.Queries;

public record GetRouteQuery(double startLat, double startLng, double endLat, double endLng):IRequest<RouteResponse>;