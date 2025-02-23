using DriverService.Core.Features.Driver.Hnadlers;
using DriverService.Core.Features.Driver.Queries;
using DriverService.Domain.Entities;
using DriverService.Domain.Exceptions;
using DriverService.Domain.Interfaces;
using Moq;

namespace DriverService.Core.Tests.HandlerTests;


public class GetDriverLocationQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenDriverExists_ShouldReturnDriverLocation()
    {
        // Arrange
        var driver = new Driver { Id = Guid.NewGuid(), CurrentLatitude = 10.0, CurrentLongitude = 20.0 };
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(driver.Id)).ReturnsAsync(driver);

        var handler = new GetDriverLocationQueryHandler(mockRepo.Object);
        var query = new GetDriverLocationQuery(driver.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(driver.CurrentLatitude, result.Latitude);
        Assert.Equal(driver.CurrentLongitude, result.Longitude);
    }

    [Fact]
    public async Task Handle_WhenDriverDoesNotExist_ShouldThrowDriverNotFoundException()
    {
        // Arrange
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Driver)null);

        var handler = new GetDriverLocationQueryHandler(mockRepo.Object);
        var query = new GetDriverLocationQuery(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<DriverNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }
}