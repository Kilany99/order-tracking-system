using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.API.Consumers;
using NotificationService.Application.Tests.Fixtures;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Events;
using NotificationService.Domain.Interfaces;
using NotificationService.Infrastructure.Channels;
using NotificationService.Infrastructure.Serialization;
using System.Text.Json;


namespace NotificationService.Application.Tests.Consumers;


public class NotificationConsumerServiceTests : IClassFixture<KafkaFixture>
{
    private readonly KafkaFixture _kafkaFixture;
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<NotificationConsumerService>> _loggerMock;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<OrderEventDeserializer>> _deserializedMockLogger;

    public NotificationConsumerServiceTests(KafkaFixture kafkaFixture)
    {
        _kafkaFixture = kafkaFixture;
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<NotificationConsumerService>>();
        _deserializedMockLogger = new Mock<ILogger<OrderEventDeserializer>>();

        var configurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Kafka:BootstrapServers"] = _kafkaFixture.KafkaContainer.GetBootstrapAddress(),
                ["Kafka:GroupId"] = "test-group"
            });
        _configuration = configurationBuilder.Build();
    }

    [Fact]
    public async Task ShouldConsumeOrderCreatedMessage()
    {
        // Arrange
        var notificationHandlerMock = new Mock<INotificationHandler>();
        var scopeMock = new Mock<IServiceScope>();
        var serviceScopeMock = new Mock<IServiceProvider>();

        serviceScopeMock
            .Setup(x => x.GetService(typeof(INotificationHandler)))
            .Returns(notificationHandlerMock.Object);

        scopeMock
            .Setup(x => x.ServiceProvider)
            .Returns(serviceScopeMock.Object);

        _scopeFactoryMock
            .Setup(x => x.CreateScope())
            .Returns(scopeMock.Object);

        var orderCreatedChannel = new NotificationChannel<OrderNotificationEvent>();
        var driverAssignedChannel = new NotificationChannel<OrderNotificationEvent>();
        var orderPickupChannel = new NotificationChannel<OrderNotificationEvent>();
        var orderDeliveredChannel = new NotificationChannel<OrderNotificationEvent>();

        var deserializer = new OrderEventDeserializer(_deserializedMockLogger.Object);

        var service = new NotificationConsumerService(
            _loggerMock.Object,
            _scopeFactoryMock.Object,
            _configuration,
            deserializer,
            orderCreatedChannel,
            driverAssignedChannel,
            orderPickupChannel,
            orderDeliveredChannel);

        // Create a producer to send test messages
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _kafkaFixture.KafkaContainer.GetBootstrapAddress()
        };

        var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        // Act
        var cts = new CancellationTokenSource();
        var serviceTask = service.StartAsync(cts.Token);

        // Send test message
        var orderEvent = new OrderNotificationEvent
        {
            OrderId = Guid.NewGuid(),
            CustomerEmail = "test@example.com",
            CustomerName = "Test Customer",
            Status = OrderStatus.Created
        };

        await producer.ProduceAsync("OrderCreated",
            new Message<string, string>
            {
                Value = JsonSerializer.Serialize(orderEvent)
            });

        // Wait for processing
        await Task.Delay(1000);
        await service.StopAsync(CancellationToken.None);

        // Assert
        notificationHandlerMock.Verify(
            x => x.HandleOrderNotificationAsync(
                It.Is<OrderNotificationEvent>(e =>
                    e.OrderId == orderEvent.OrderId &&
                    e.Status == OrderStatus.Created)),
            Times.Once);
    }
}
