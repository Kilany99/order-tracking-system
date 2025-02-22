using DriverService.Core.Dtos;
using DriverService.Domain.Entities;
using MediatR;

namespace DriverService.Core.Features.Driver.Commands;

public record AuthRegisterDriverCommand(
    string Name,
    string Email,
    string Password,
    string VehicleType) : IRequest<DriverResponse>;

public record LoginDriverCommand(
    string Email,
    string Password) : IRequest<AuthResponse>;

