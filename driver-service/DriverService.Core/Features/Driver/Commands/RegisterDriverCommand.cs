

using DriverService.Core.Dtos;
using MediatR;

namespace DriverService.Core.Features.Driver.Commands;

public record RegisterDriverCommand(
    string Name,
    string Email,       
    string Password,    
    string VehicleType) : IRequest<DriverResponse>;
