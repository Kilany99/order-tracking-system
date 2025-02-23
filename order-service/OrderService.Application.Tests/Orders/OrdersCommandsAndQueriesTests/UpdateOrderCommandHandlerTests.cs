using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Application.Features.Orders.Commands;
using OrderService.Application.Features.Orders.Handlers;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Repositories;


namespace OrderService.Application.Tests.Orders.OrdersCommandsAndQueriesTests
{
    public class UpdateOrderCommandHandlerTests
    {
        private readonly Mock<IOrderRepository> _mockRepo = new();
        private readonly Mock<ILogger<UpdateOrderStatusCommand>> _mockLogger = new();
        private readonly UpdateOrderStatusCommandHandler _handler;

        public UpdateOrderCommandHandlerTests()
        {
            _handler = new UpdateOrderStatusCommandHandler(_mockRepo.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ShouldUpdateOrder_WhenCommandIsValid()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var existingOrder = Order.Create("cust-123", "123 Main St", 55.3, 88.3);
            // Normally the repository is responsible for setting the order's ID,
            // so we simulate that by setting it manually:
            existingOrder.GetType().GetProperty("Id")!.SetValue(existingOrder, orderId);

            // Setup repository to return the existing order
            _mockRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(existingOrder);

            // Create an update command (assuming it changes the delivery address)
            var command = new UpdateOrderStatusCommand(orderId, OrderStatus.Created);

            // Act
            var resultId = await _handler.Handle(command, CancellationToken.None);

            // Assert
            resultId.Should().Be(orderId);
            existingOrder.Status.Should().Be(OrderStatus.Created);
            _mockRepo.Verify(r => r.UpdateAsync(existingOrder), Times.Once);
        }
    }
}
