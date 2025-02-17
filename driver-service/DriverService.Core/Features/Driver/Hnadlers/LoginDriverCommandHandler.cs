using DriverService.Core.Dtos;
using DriverService.Core.Features.Driver.Commands;
using DriverService.Domain.Entities;
using DriverService.Domain.Exceptions;
using DriverService.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace DriverService.Core.Features.Driver.Hnadlers;

public class LoginDriverCommandHandler
    : IRequestHandler<LoginDriverCommand, AuthResponse>
{
    private readonly IDriverRepository _repository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher<DriverAuth> _passwordHasher;

    public LoginDriverCommandHandler(
        IDriverRepository repository,
        IJwtService jwtService,
        IPasswordHasher<DriverAuth> passwordHasher)
    {
        _repository = repository;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponse> Handle(
        LoginDriverCommand request,
        CancellationToken cancellationToken)
    {
        var driverAuth = await _repository.GetAuthByEmailAsync(request.Email)
        ?? throw new InvalidCredentialsException();

        var verificationResult = _passwordHasher.VerifyHashedPassword(
            driverAuth,
            driverAuth.PasswordHash,
            request.Password);

        if (verificationResult != PasswordVerificationResult.Success)
            throw new InvalidCredentialsException();

        var driver = await _repository.GetByIdAsync(driverAuth.DriverId)
            ?? throw new DriverNotFoundException(driverAuth.DriverId);

        var accessToken = _jwtService.GenerateToken(driver, driverAuth);
        var refreshToken = _jwtService.GenerateRefreshToken();

        await _repository.SaveRefreshTokenAsync(
            driver.Id,
            refreshToken.Token,
            refreshToken.Expires);

        return new AuthResponse(
            driver.Id,
            accessToken,
            refreshToken.Token,
            refreshToken.Expires);
    }
}