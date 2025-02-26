// NotificationService.Infrastructure/Services/GmailService.cs
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Interfaces;
using NotificationService.Domain.Settings;

namespace NotificationService.Infrastructure.Services;

public class GmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<GmailService> _logger;

    public GmailService(
        IOptions<EmailSettings> emailSettings,
        ILogger<GmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            using var message = new MailMessage();
            message.From = new MailAddress("no-reply@deliverysystem.net","Talaby System");
            message.To.Add(new MailAddress(to));
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            using var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(_emailSettings.GmailUser, _emailSettings.GmailAppPassword),
                EnableSsl = true,
            };

            await smtpClient.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {RecipientEmail}", to);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {RecipientEmail}", to);
            throw;
        }
    }
}