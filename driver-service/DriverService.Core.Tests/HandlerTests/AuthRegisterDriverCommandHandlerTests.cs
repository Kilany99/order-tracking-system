using DriverService.Core.Features.Driver.Commands;
using DriverService.Core.Features.Driver.Hnadlers;
using DriverService.Domain.Entities;
using DriverService.Domain.Exceptions;
using DriverService.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace DriverService.Core.Tests.HandlerTests;

public class AuthRegisterDriverCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenDriverDoesNotExist_ShouldRegisterDriver()
    {
        // Arrange
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.DriverExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
        var mockHasher = new Mock<IPasswordHasher<DriverAuth>>();
        mockHasher.Setup(h => h.HashPassword(It.IsAny<DriverAuth>(), It.IsAny<string>())).Returns("hashedPassword");

        var handler = new AuthRegisterDriverCommandHandler(mockRepo.Object, mockHasher.Object);
        var command = new RegisterDriverCommand
        ("Test Driver","test@example.com","password", "Car");


        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        mockRepo.Verify(r => r.AddAsync(It.Is<Driver>(d => d.Name == "Test Driver" && d.VehicleType == "Car")), Times.Once);
        mockRepo.Verify(r => r.AddAuthAsync(It.Is<DriverAuth>(a => a.Email == "test@example.com" && a.PasswordHash == "hashedPassword")), Times.Once);
        mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        Assert.NotNull(result);
        Assert.Equal("Test Driver", result.Name);
    }

    [Fact]
    public async Task Handle_WhenDriverExists_ShouldThrowDuplicateDriverException()
    {
        // Arrange
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.DriverExistsAsync(It.IsAny<string>())).ReturnsAsync(true);
        var mockHasher = new Mock<IPasswordHasher<DriverAuth>>();

        var handler = new AuthRegisterDriverCommandHandler(mockRepo.Object, mockHasher.Object);
        var command = new RegisterDriverCommand
        ("test@example.com", "Test Driver", "password", "Car");

        // Act & Assert
        await Assert.ThrowsAsync<DuplicateDriverException>(() => handler.Handle(command, CancellationToken.None));
        mockRepo.Verify(r => r.AddAsync(It.IsAny<Driver>()), Times.Never);
        mockRepo.Verify(r => r.AddAuthAsync(It.IsAny<DriverAuth>()), Times.Never);
    }
}