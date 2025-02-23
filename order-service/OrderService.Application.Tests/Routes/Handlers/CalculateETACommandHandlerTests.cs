using MediatR;
using Moq;
using OrderService.Application.Features.Routes.Commands;
using OrderService.Application.Features.Routes.Handlers;
using OrderService.Infrastructure.Routing.Interfaces;
using System;

namespace OrderService.Application.Tests.Routes.Handlers;


/// <summary>
/// Success Case: Verifies that the handler calls CalculateETA on the IRoutingService with the correct parameters and returns the expected TimeSpan.
/// Error Case: Ensures that exceptions thrown by the IRoutingService are propagated to the caller.
/// </summary>
public class CalculateETACommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnETAFromRoutingService()
    {
        // Arrange
        var mockRoutingService = new Mock<IRoutingService>();
        var expectedETA = TimeSpan.FromMinutes(30);
        mockRoutingService.Setup(s => s.CalculateETA(1.0, 2.0, 3.0, 4.0))
                          .ReturnsAsync(expectedETA);

        var handler = new CalculateETACommandHandler(mockRoutingService.Object);
        var command = new CalculateETACommand( 1.0, 2.0, 3.0, 4.0);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(expectedETA, result);
        mockRoutingService.Verify(s => s.CalculateETA(1.0, 2.0, 3.0, 4.0), Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_WhenServiceFails()
    {
        // Arrange
        var mockRoutingService = new Mock<IRoutingService>();
        mockRoutingService.Setup(s => s.CalculateETA(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                          .ThrowsAsync(new Exception("Service failed"));

        var handler = new CalculateETACommandHandler(mockRoutingService.Object);
        var command = new CalculateETACommand(1.0, 2.0, 3.0, 4.0);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => handler.Handle(command, CancellationToken.None));
    }
}