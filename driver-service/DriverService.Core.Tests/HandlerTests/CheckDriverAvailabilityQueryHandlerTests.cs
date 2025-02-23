using DriverService.Core.Features.Driver.Handlers;
using DriverService.Core.Features.Driver.Queries;
using DriverService.Domain.Entities;
using DriverService.Domain.Interfaces;
using Moq;

namespace DriverService.Core.Tests.HandlerTests;

public class CheckDriverAvailabilityQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenDriverExistsAndIsAvailable_ShouldReturnTrue()
    {
        // Arrange
        var driver = new Driver { Id = Guid.NewGuid(), IsAvailable = true };
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(driver.Id)).ReturnsAsync(driver);

        var handler = new CheckDriverAvailabilityQueryHandler(mockRepo.Object);
        var query = new CheckDriverAvailabilityQuery(driver.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task Handle_WhenDriverExistsButIsNotAvailable_ShouldReturnFalse()
    {
        // Arrange
        var driver = new Driver { Id = Guid.NewGuid(), IsAvailable = false };
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(driver.Id)).ReturnsAsync(driver);

        var handler = new CheckDriverAvailabilityQueryHandler(mockRepo.Object);
        var query = new CheckDriverAvailabilityQuery(driver.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Handle_WhenDriverDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Driver)null);

        var handler = new CheckDriverAvailabilityQueryHandler(mockRepo.Object);
        var query = new CheckDriverAvailabilityQuery(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result);
    }
}