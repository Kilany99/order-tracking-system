using DriverService.Core.Dtos;
using DriverService.Core.Features.Driver.Commands;
using DriverService.Domain.Entities;
using DriverService.Domain.Exceptions;
using DriverService.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace DriverService.Core.Features.Driver.Hnadlers;
public class AuthRegisterDriverCommandHandler
    : IRequestHandler<RegisterDriverCommand, DriverResponse>
{
    private readonly IDriverRepository _repository;
    private readonly IPasswordHasher<DriverAuth> _passwordHasher;

    public AuthRegisterDriverCommandHandler(
        IDriverRepository repository,
        IPasswordHasher<DriverAuth> passwordHasher)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
    }

    public async Task<DriverResponse> Handle(
        RegisterDriverCommand request,
        CancellationToken cancellationToken)
    {
        if (await _repository.DriverExistsAsync(request.Email))
            throw new DuplicateDriverException(request.Email);

        var driver = new Domain.Entities.Driver
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            VehicleType = request.VehicleType,
            IsAvailable = true,
            RegistrationDate = DateTime.UtcNow
        };

        var auth = new DriverAuth
        {
            DriverId = driver.Id,
            Email = request.Email
        };

        auth.PasswordHash = _passwordHasher.HashPassword(auth, request.Password);

        await _repository.AddAsync(driver);
        await _repository.AddAuthAsync(auth);
        await _repository.SaveChangesAsync();

        return MapToResponse(driver);
    }

    private static DriverResponse MapToResponse(Domain.Entities.Driver driver)
    {
        return new DriverResponse(
            driver.Id,
            driver.Name,
            driver.VehicleType,
            driver.IsAvailable);
    }
}