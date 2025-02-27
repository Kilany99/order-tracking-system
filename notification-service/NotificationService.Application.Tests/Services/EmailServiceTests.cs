using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NotificationService.Domain.Settings;
using NotificationService.Infrastructure.Services;
using NotificationService.Application.Tests.Fixtures;
using Xunit;

namespace NotificationService.Application.Tests.Services
{
    public class EmailServiceTests : IClassFixture<TestEmailServer>, IDisposable
    {
        private readonly TestEmailServer _emailServer;
        private readonly ILogger<GmailService> _logger;
        private readonly EmailSettings _settings;

        public EmailServiceTests(TestEmailServer emailServer)
        {
            _emailServer = emailServer;
            _logger = Mock.Of<ILogger<GmailService>>();
            _settings = new EmailSettings
            {
                GmailUser = "test@example.com",
                GmailAppPassword = "testpassword"
            };
        }

        public void Dispose()
        {
            _emailServer.ClearReceivedEmails();
        }

        [Fact]
        public async Task SendEmailAsync_ShouldSendEmail_WhenValidParameters()
        {
            // Arrange
            var service = new GmailService(Options.Create(_settings), _logger);
            var to = "recipient@example.com";
            var subject = "Test Subject";
            var body = "Test Body";

            // Act
            await service.SendEmailAsync(to, subject, body);

            // Assert
            _emailServer.ReceivedEmails.Should().HaveCount(1);
            var message = _emailServer.ReceivedEmails.First();
            message.ToAddresses.Should().Contain(address => address.ToString() == to);
            message.Subject.Should().Be(subject);
            message.MessageParts.Should().Contain(part => part.BodyData == body);
        }

        [Fact]
        public async Task SendEmailAsync_ShouldSetCorrectSender_WhenEmailIsSent()
        {
            // Arrange
            var service = new GmailService(Options.Create(_settings), _logger);
            var to = "recipient@example.com";
            var subject = "Test Subject";
            var body = "Test Body";

            // Act
            await service.SendEmailAsync(to, subject, body);

            // Assert
            var message = _emailServer.ReceivedEmails.Should().HaveCount(1).And.Subject.First();
            message.FromAddress.ToString().Should().Be(_settings.GmailUser);
        }

        [Fact]
        public async Task SendEmailAsync_ShouldHandleHtmlContent_WhenBodyContainsHtml()
        {
            // Arrange
            var service = new GmailService(Options.Create(_settings), _logger);
            var to = "recipient@example.com";
            var subject = "Test Subject";
            var htmlBody = "<html><body><h1>Test</h1><p>HTML Content</p></body></html>";

            // Act
            await service.SendEmailAsync(to, subject, htmlBody);

            // Assert
            var message = _emailServer.ReceivedEmails.Should().HaveCount(1).And.Subject.First();
            message.MessageParts.Should().Contain(part =>
                part.BodyData.Contains("HTML Content") &&
                part.BodyData.Contains("<html>"));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("invalid-email")]
        public async Task SendEmailAsync_ShouldThrowException_WhenEmailAddressIsInvalid(string invalidEmail)
        {
            // Arrange
            var service = new GmailService(Options.Create(_settings), _logger);
            var subject = "Test Subject";
            var body = "Test Body";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.SendEmailAsync(invalidEmail, subject, body));
        }

        [Fact]
        public async Task SendEmailAsync_ShouldLogError_WhenSmtpServerIsUnavailable()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<GmailService>>();
            var invalidSettings = new EmailSettings
            {
                GmailUser = "test@example.com",
                GmailAppPassword = "wrong-password"
            };

            var service = new GmailService(Options.Create(invalidSettings), loggerMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                service.SendEmailAsync("test@example.com", "Subject", "Body"));

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }
    }
}