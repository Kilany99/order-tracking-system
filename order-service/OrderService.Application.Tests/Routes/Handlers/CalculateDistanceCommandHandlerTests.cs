using Moq;
using OrderService.Application.Features.Routes.Commands;
using OrderService.Application.Features.Routes.Handlers;
using OrderService.Infrastructure.Routing.Interfaces;
using System;


namespace OrderService.Application.Tests.Routes.Handlers;

/// <summary>
/// Success Case: Confirms that the handler calls CalculateDistance and returns the expected distance.
/// Error Case: Verifies that exceptions from the IRoutingService are not swallowed.
/// </summary>
public class CalculateDistanceCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnDistanceFromRoutingService()
    {
        // Arrange
        var mockRoutingService = new Mock<IRoutingService>();
        var expectedDistance = 1000.0;
        mockRoutingService.Setup(s => s.CalculateDistance(1.0, 2.0, 3.0, 4.0))
                          .ReturnsAsync(expectedDistance);

        var handler = new CalculateDistanceCommandHandler(mockRoutingService.Object);
        var command = new CalculateDistanceCommand(1.0, 2.0, 3.0, 4.0);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(expectedDistance, result);
        mockRoutingService.Verify(s => s.CalculateDistance(1.0, 2.0, 3.0, 4.0), Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_WhenServiceFails()
    {
        // Arrange
        var mockRoutingService = new Mock<IRoutingService>();
        mockRoutingService.Setup(s => s.CalculateDistance(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                          .ThrowsAsync(new Exception("Service failed"));

        var handler = new CalculateDistanceCommandHandler(mockRoutingService.Object);
        var command = new CalculateDistanceCommand(1.0, 2.0, 3.0, 4.0);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => handler.Handle(command, CancellationToken.None));
    }
}