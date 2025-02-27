using Microsoft.Extensions.Logging;
using Moq;
using NotificationService.Application.Services;
using NotificationService.Domain.Enums;
using NotificationService.Domain.Events;
using NotificationService.Domain.Interfaces;

namespace NotificationService.Application.Tests.Handlers;


public class NotificationHandlerTests
{
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ICustomerService> _customerServiceMock;
    private readonly Mock<ILogger<NotificationHandler>> _loggerMock;
    private readonly NotificationHandler _handler;

    public NotificationHandlerTests()
    {
        _emailServiceMock = new Mock<IEmailService>();
        _customerServiceMock = new Mock<ICustomerService>();
        _loggerMock = new Mock<ILogger<NotificationHandler>>();
        _handler = new NotificationHandler(
            _emailServiceMock.Object,
            _customerServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleOrderNotificationAsync_ShouldSendEmail_ForOrderCreated()
    {
        // Arrange
        var notification = new OrderNotificationEvent
        {
            OrderId = Guid.NewGuid(),
            CustomerEmail = "test@example.com",
            CustomerName = "Test Customer",
            Status = OrderStatus.Created
        };

        // Act
        await _handler.HandleOrderNotificationAsync(notification);

        // Assert
        _emailServiceMock.Verify(
            x => x.SendEmailAsync(
                notification.CustomerEmail,
                It.Is<string>(s => s.Contains("Order Confirmation")),
                It.Is<string>(b => b.Contains(notification.CustomerName))),
            Times.Once);
    }
}
