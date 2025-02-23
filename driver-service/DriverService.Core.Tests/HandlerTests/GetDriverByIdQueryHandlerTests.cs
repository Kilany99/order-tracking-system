
using DriverService.Core.Features.Driver.Handlers;
using DriverService.Core.Features.Driver.Queries;
using DriverService.Domain.Entities;
using DriverService.Domain.Exceptions;
using DriverService.Domain.Interfaces;
using Moq;

namespace DriverService.Core.Tests.HandlerTests;

public class GetDriverByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenDriverExists_ShouldReturnDriverResponse()
    {
        // Arrange
        var driver = new Driver { Id = Guid.NewGuid(), Name = "Test Driver", VehicleType = "Car", IsAvailable = true };
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(driver.Id)).ReturnsAsync(driver);

        var handler = new GetDriverByIdQueryHandler(mockRepo.Object);
        var query = new GetDriverByIdQuery(driver.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(driver.Id, result.Id);
        Assert.Equal(driver.Name, result.Name);
    }

    [Fact]
    public async Task Handle_WhenDriverDoesNotExist_ShouldThrowDriverNotFoundException()
    {
        // Arrange
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Driver)null);

        var handler = new GetDriverByIdQueryHandler(mockRepo.Object);
        var query = new GetDriverByIdQuery(Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<DriverNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }
}