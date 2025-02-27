using netDumbster.smtp;
using System;

namespace NotificationService.Application.Tests.Fixtures
{
    public class TestEmailServer : IDisposable
    {
        public SimpleSmtpServer SmtpServer { get; }

        public TestEmailServer()
        {
            SmtpServer = SimpleSmtpServer.Start(25); // Default port
        }

        public SmtpMessage[] ReceivedEmails => SmtpServer.ReceivedEmail;

        public void ClearReceivedEmails()
        {
            SmtpServer.ClearReceivedEmail();
        }

        public void Dispose()
        {
            SmtpServer?.Stop();
            SmtpServer?.Dispose();
        }
    }
}
