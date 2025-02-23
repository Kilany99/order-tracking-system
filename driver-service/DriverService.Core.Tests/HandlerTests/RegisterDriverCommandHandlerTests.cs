using DriverService.Core.Features.Driver.Commands;
using DriverService.Core.Features.Driver.Hnadlers;
using DriverService.Domain.Entities;
using DriverService.Domain.Interfaces;
using Moq;


namespace DriverService.Core.Tests.HandlerTests;

//This handler registers a driver without authentication (simpler version).

public class RegisterDriverCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldRegisterDriver()
    {
        // Arrange
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.AddAsync(It.IsAny<Driver>())).Returns(Task.CompletedTask);
        mockRepo.Setup(r => r.SaveChangesAsync()).Returns((Task<bool>)Task.CompletedTask);

        var handler = new RegisterDriverCommandHandler(mockRepo.Object);
        var command = new RegisterDriverCommand("Test","text@example.com","Password","Car");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        mockRepo.Verify(r => r.AddAsync(It.Is<Driver>(d => d.Name == "Test Driver" && d.VehicleType == "Car")), Times.Once);
        mockRepo.Verify(r => r.SaveChangesAsync(), Times.Once);
        Assert.NotNull(result);
        Assert.Equal("Test Driver", result.Name);
    }
}