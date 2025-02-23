using Moq;
using OrderService.Application.Features.Routes.Handlers;
using OrderService.Application.Features.Routes.Queries;
using OrderService.Infrastructure.Routing.Interfaces;
using OrderService.Infrastructure.Routing.Models;

namespace OrderService.Application.Tests.Routes.Handlers;

public class GetRouteQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnRouteFromRoutingService()
    {
        // Arrange
        var mockRoutingService = new Mock<IRoutingService>();
        var expectedRoute = new RouteResponse { Distance = 1000, Duration = 600 };
        mockRoutingService.Setup(s => s.GetRoute(1.0, 2.0, 3.0, 4.0))
                          .ReturnsAsync(expectedRoute);

        var handler = new GetRouteQueryHandler(mockRoutingService.Object);
        var query = new GetRouteQuery(1.0,  2.0, 3.0, 4.0);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(expectedRoute, result);
        mockRoutingService.Verify(s => s.GetRoute(1.0, 2.0, 3.0, 4.0), Times.Once());
    }

    [Fact]
    public async Task Handle_ShouldPropagateException_WhenServiceFails()
    {
        // Arrange
        var mockRoutingService = new Mock<IRoutingService>();
        mockRoutingService.Setup(s => s.GetRoute(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>()))
                          .ThrowsAsync(new Exception("Service failed"));

        var handler = new GetRouteQueryHandler(mockRoutingService.Object);
        var query = new GetRouteQuery(1.0, 2.0, 3.0, 4.0);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => handler.Handle(query, CancellationToken.None));
    }
}