using MediatR;

namespace DriverService.Core.Features.Driver.Queries;

public record FindNearestDriverQuery(double Latitude, double Longitude) : IRequest<Guid>;
