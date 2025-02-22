using FluentAssertions;
using Moq;
using OrderService.Application.Features.Orders.Queries;
using OrderService.Application.Features.Orders.Handlers;
using OrderService.Infrastructure.Repositories;
using OrderService.Domain.Entities;

namespace OrderService.Application.Tests
{
    public class GetOrderByIdQueryHandlerTests
    {
        private readonly Mock<IOrderRepository> _mockRepo = new();
        private readonly GetOrderByIdQueryHandler _handler;

        public GetOrderByIdQueryHandlerTests()
        {
            _handler = new GetOrderByIdQueryHandler(_mockRepo.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnOrder_WhenOrderExistsAndDontHaveDriverId()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var existingOrder = Order.Create("cust-123", "123 Main St",3.5,665.3);
            // Simulate setting the order's ID
            existingOrder.GetType().GetProperty("Id")!.SetValue(existingOrder, orderId);
            _mockRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(existingOrder);

            var query = new GetOrderByIdQuery(orderId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(orderId);
        }

        [Fact]
        public async Task Handle_ShouldReturnOrder_WhenOrderExistsAndHasDriverId()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var existingOrder = Order.Create("cust-123", "123 Main St", 3.5, 665.3);
            existingOrder.SetDriverId(Guid.NewGuid());
            // Simulate setting the order's ID
            existingOrder.GetType().GetProperty("Id")!.SetValue(existingOrder, orderId);
            _mockRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(existingOrder);

            var query = new GetOrderByIdQuery(orderId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(orderId);
            result.DriverId.Should().NotBeNull();
        }
    }
}

