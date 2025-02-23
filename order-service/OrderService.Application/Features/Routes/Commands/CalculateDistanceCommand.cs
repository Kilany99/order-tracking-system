
using MediatR;

namespace OrderService.Application.Features.Routes.Commands;


public record CalculateDistanceCommand(double startLat, double startLng, double endLat, double endLng) : IRequest<double>;
