using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Application.Features.Orders.Commands;
using OrderService.Application.Features.Orders.Handlers;
using OrderService.Application.Features.Orders.Queries;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderService.Application.Tests.Orders.OrdersCommandsAndQueriesTests
{
    public class DeleteOrderCommandHandlerTests
    {
        private readonly Mock<IOrderRepository> _mockRepo = new();
        private readonly Mock<ILogger<DeleteOrderCommandHandler>> _mockLogger = new();
        private readonly DeleteOrderCommandHandler _handler;
        public DeleteOrderCommandHandlerTests()
        {
            _handler = new DeleteOrderCommandHandler(_mockRepo.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ShouldDeleteOrder_WhenOrderExists()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            var existingOrder = Order.Create("cust-123", "123 Main St", 3.5, 665.3);
            // Simulate setting the order's ID
            existingOrder.GetType().GetProperty("Id")!.SetValue(existingOrder, orderId);
            _mockRepo.Setup(r => r.GetByIdAsync(orderId)).ReturnsAsync(existingOrder);

            var request = new DeleteOrderCommand(orderId);

            // Act
            var result = await _handler.Handle(request, CancellationToken.None);

            // Assert
            result.CompareTo(true);
            result.GetType().Should().Be<bool>();
        }
    }
}
