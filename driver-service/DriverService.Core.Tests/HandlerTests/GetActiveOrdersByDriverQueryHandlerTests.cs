using DriverService.Core.Features.Driver.Handlers;
using DriverService.Core.Features.Driver.Queries;
using DriverService.Domain.Entities;
using DriverService.Domain.Exceptions;
using DriverService.Domain.Interfaces;
using Moq;

namespace DriverService.Core.Tests.HandlerTests;


public class GetActiveOrdersByDriverQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenDriverHasActiveOrder_ShouldReturnOrderResponse()
    {
        // Arrange
        var driver = new Driver { Id = Guid.NewGuid(), CurrentOrderId = Guid.NewGuid() };
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(driver.Id)).ReturnsAsync(driver);

        var handler = new GetActiveOrdersByDriverQueryHandler(mockRepo.Object);
        var query = new GetActiveOrdersByDriverQuery(driver.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(driver.CurrentOrderId, result.OrderId);
    }

    [Fact]
    public async Task Handle_WhenDriverHasNoActiveOrder_ShouldThrowDriverNotFoundException()
    {
        // Arrange
        var driver = new Driver { Id = Guid.NewGuid(), CurrentOrderId = null };
        var mockRepo = new Mock<IDriverRepository>();
        mockRepo.Setup(r => r.GetByIdAsync(driver.Id)).ReturnsAsync(driver);

        var handler = new GetActiveOrdersByDriverQueryHandler(mockRepo.Object);
        var query = new GetActiveOrdersByDriverQuery(driver.Id);

        // Act & Assert
        await Assert.ThrowsAsync<DriverNotFoundException>(() => handler.Handle(query, CancellationToken.None));
    }
}