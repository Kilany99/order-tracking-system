using DriverService.Domain.Models;
using MediatR;


namespace DriverService.Core.Features.Driver.Queries;

public record GetDriversBatchQuery(List<Guid> DriverIds) : IRequest<List<DriverDetailsDto>>;
