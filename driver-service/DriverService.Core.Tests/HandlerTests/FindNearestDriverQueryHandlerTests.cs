
using DriverService.Core.Features.Driver.Hnadlers;
using DriverService.Core.Features.Driver.Queries;
using DriverService.Domain.Entities;
using DriverService.Domain.Exceptions;
using DriverService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace DriverService.Core.Tests.HandlerTests;

public class FindNearestDriverQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenNearestDriverFound_ShouldReturnDriverId()
    {
        // Arrange
        var driver = new Driver { Id = Guid.NewGuid() };
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.FindNearestDriverAsync(It.IsAny<double>(), It.IsAny<double>())).ReturnsAsync(driver);

        var handler = new FindNearestDriverQueryHandler(mockRepo.Object, Mock.Of<ILogger<FindNearestDriverQueryHandler>>());
        var query = new FindNearestDriverQuery(0,0);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(driver.Id, result);
    }

    [Fact]
    public async Task Handle_WhenNoDriversFound_ShouldThrowDriverNotFoundException()
    {
        // Arrange
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.FindNearestDriverAsync(It.IsAny<double>(), It.IsAny<double>())).ReturnsAsync((Driver)null);

        var handler = new FindNearestDriverQueryHandler(mockRepo.Object, Mock.Of<ILogger<FindNearestDriverQueryHandler>>());
        var query = new FindNearestDriverQuery(0, 0);

        // Act & Assert
        await Assert.ThrowsAsync<DriverNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }
}