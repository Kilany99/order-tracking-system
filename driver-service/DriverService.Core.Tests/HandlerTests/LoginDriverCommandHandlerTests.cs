using DriverService.Core.Features.Driver.Commands;
using DriverService.Core.Features.Driver.Hnadlers;
using DriverService.Domain.Entities;
using DriverService.Domain.Exceptions;
using DriverService.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace DriverService.Core.Tests.HandlerTests;


public class LoginDriverCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCredentialsAreCorrect_ShouldReturnAuthResponse()
    {
        // Arrange
        var driver = new Driver { Id = Guid.NewGuid() };
        var driverAuth = new DriverAuth { DriverId = driver.Id, Email = "test@example.com", PasswordHash = "hashedPassword" };
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.GetAuthByEmailAsync("test@example.com")).ReturnsAsync(driverAuth);
        mockRepo.Setup(r => r.GetByIdAsync(driver.Id)).ReturnsAsync(driver);

        var mockHasher = new Mock<IPasswordHasher<DriverAuth>>();
        mockHasher.Setup(h => h.VerifyHashedPassword(driverAuth, "hashedPassword", "password")).Returns(PasswordVerificationResult.Success);

        var mockJwtService = new Mock<IJwtService>();
        mockJwtService.Setup(j => j.GenerateToken(driver, driverAuth)).Returns("accessToken");
        mockJwtService.Setup(j => j.GenerateRefreshToken()).Returns(new RefreshToken { Token = "refreshToken", Expires = DateTime.UtcNow.AddDays(7) });

        var handler = new LoginDriverCommandHandler(mockRepo.Object, mockJwtService.Object, mockHasher.Object);
        var command = new LoginDriverCommand("test@example.com", "password");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(driver.Id, result.DriverId);
        Assert.Equal("accessToken", result.AccessToken);
        Assert.Equal("refreshToken", result.RefreshToken);
        mockRepo.Verify(r => r.SaveRefreshTokenAsync(driver.Id, "refreshToken", It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPasswordIsIncorrect_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        var driverAuth = new DriverAuth { Email = "test@example.com", PasswordHash = "hashedPassword" };
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.GetAuthByEmailAsync("test@example.com")).ReturnsAsync(driverAuth);

        var mockHasher = new Mock<IPasswordHasher<DriverAuth>>();
        mockHasher.Setup(h => h.VerifyHashedPassword(driverAuth, "hashedPassword", "wrongPassword")).Returns(PasswordVerificationResult.Failed);

        var handler = new LoginDriverCommandHandler(mockRepo.Object, Mock.Of<IJwtService>(), mockHasher.Object);
        var command = new LoginDriverCommand("test@example.com", "password");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenEmailDoesNotExist_ShouldThrowInvalidCredentialsException()
    {
        // Arrange
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.GetAuthByEmailAsync("test@example.com")).ReturnsAsync((DriverAuth)null);

        var handler = new LoginDriverCommandHandler(mockRepo.Object, Mock.Of<IJwtService>(), Mock.Of<IPasswordHasher<DriverAuth>>());
        var command = new LoginDriverCommand("test@example.com", "password");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidCredentialsException>(() => handler.Handle(command, CancellationToken.None));
    }
}