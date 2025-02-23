
using MediatR;

namespace OrderService.Application.Features.Routes.Commands;

public record CalculateETACommand(double startLat, double startLng, double endLat, double endLng): IRequest<TimeSpan>;