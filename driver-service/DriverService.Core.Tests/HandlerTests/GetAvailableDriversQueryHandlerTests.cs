using DriverService.Core.Features.Driver.Queries;
using DriverService.Core.QueryHandlers;
using DriverService.Domain.Entities;
using DriverService.Domain.Interfaces;
using Moq;


namespace DriverService.Core.Tests.HandlerTests;

public class GetAvailableDriversQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnAvailableDrivers()
    {
        // Arrange
        var drivers = new List<Driver>
        {
            new Driver { Id = Guid.NewGuid(), Name = "Driver1", IsAvailable = true },
            new Driver { Id = Guid.NewGuid(), Name = "Driver2", IsAvailable = true }
        };
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.GetAvailableDriversAsync()).ReturnsAsync(drivers);

        var handler = new GetAvailableDriversQueryHandler(mockRepo.Object);
        var query = new GetAvailableDriversQuery();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.All(result, r => Assert.True(r.IsAvailable));
    }
}