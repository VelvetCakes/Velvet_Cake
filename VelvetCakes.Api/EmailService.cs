using System.Net;
using System.Net.Mail;

namespace VelvetCakes.Api.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var smtpServer = _config["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "2525");
            var smtpUser = _config["EmailSettings:SmtpUser"];
            var smtpPass = _config["EmailSettings:SmtpPass"];
            var fromEmail = _config["EmailSettings:FromEmail"];
            var fromName = _config["EmailSettings:FromName"] ?? "Velvet Кондитерская";

            if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
            {
                _logger.LogError("SMTP settings are missing");
                return false;
            }

            _logger.LogInformation($"Sending email to {to} via {smtpServer}:{smtpPort}");

            using var client = new SmtpClient(smtpServer, smtpPort);

            if (smtpPort == 465)
            {
                client.EnableSsl = true;
            }
            else
            {
                client.EnableSsl = false;
            }

            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(smtpUser, smtpPass);
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Timeout = 30000;

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(new MailAddress(to));

            await client.SendMailAsync(message);
            _logger.LogInformation($"Email sent successfully to {to}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email to {to}: {ex.Message}");
            return false;
        }
    }
}