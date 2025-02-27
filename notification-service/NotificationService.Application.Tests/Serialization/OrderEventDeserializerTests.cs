using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Domain.Enums;
using NotificationService.Infrastructure.Serialization;

using System.Text;

namespace NotificationService.Application.Tests.Serialization;


public class OrderEventDeserializerTests
{
    private readonly ILogger<OrderEventDeserializer> _logger;
    private readonly OrderEventDeserializer _deserializer;

    public OrderEventDeserializerTests()
    {
        _logger = Mock.Of<ILogger<OrderEventDeserializer>>();
        _deserializer = new OrderEventDeserializer(_logger);
    }

    [Fact]
    public void Deserialize_ShouldDeserialize_OrderCreatedEvent()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var json = $@"{{
            ""orderId"": ""{orderId}"",
            ""customerEmail"": ""test@example.com"",
            ""customerName"": ""Test Customer"",
            ""createdAt"": ""2024-02-26T00:00:00Z""
        }}";
        var data = Encoding.UTF8.GetBytes(json);

        // Act
        var result = _deserializer.Deserialize(data, false, SerializationContext.Empty);

        // Assert
        result.Should().NotBeNull();
        result.OrderId.Should().Be(orderId);
        result.CustomerEmail.Should().Be("test@example.com");
        result.CustomerName.Should().Be("Test Customer");
        result.Status.Should().Be(OrderStatus.Created);
    }
}