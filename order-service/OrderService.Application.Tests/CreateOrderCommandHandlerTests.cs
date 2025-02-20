using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Application.Features.Orders.Commands;
using OrderService.Application.Features.Orders.Handlers;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Producers;
using OrderService.Infrastructure.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OrderService.Application.Tests
{
    public class CreateOrderCommandHandlerTests
    {
        private readonly Mock<IOrderRepository> _mockRepo = new();
        private readonly Mock<ILogger<CreateOrderCommandHandler>> _mockLogger = new();
        private readonly CreateOrderCommandHandler _handler;
        private readonly Mock<IOrderCreatedProducer> _mockProducer = new();
        public CreateOrderCommandHandlerTests()
        {
            _handler = new CreateOrderCommandHandler(_mockRepo.Object, _mockLogger.Object,_mockProducer.Object);
        }

        [Fact]
        public async Task Handle_ShouldCreateOrder_WhenCommandIsValid()
        {
            // Arrange
            var command = new CreateOrderCommand("cust-123", "123 Main St", 3.5, 665.3);

            // Act
            var orderId = await _handler.Handle(command, CancellationToken.None);

            // Assert
            orderId.Should().NotBeNull();
            _mockRepo.Verify(r => r.AddAsync(It.IsAny<Order>()), Times.Once);
        }
    }
}
