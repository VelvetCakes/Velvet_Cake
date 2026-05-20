using System.Net;
using System.Net.Mail;

namespace VelvetCakes.Api.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
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

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        try
        {
            var smtpServer = _config["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(_config["EmailSettings:SmtpPort"] ?? "465");
            var smtpUser = _config["EmailSettings:SmtpUser"];
            var smtpPass = _config["EmailSettings:SmtpPass"];
            var fromEmail = _config["EmailSettings:FromEmail"];
            var fromName = _config["EmailSettings:FromName"] ?? "Velvet Кондитерская";

            var useSsl = smtpPort == 465 || smtpPort == 587;

            using var client = new SmtpClient(smtpServer, smtpPort);
            client.EnableSsl = useSsl;
            client.UseDefaultCredentials = false;
            client.Credentials = new NetworkCredential(smtpUser, smtpPass);
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Timeout = 10000;

            var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(to);

            _logger.LogInformation($"Sending email to {to} via {smtpServer}:{smtpPort}");
            await client.SendMailAsync(message);
            _logger.LogInformation($"Email sent successfully to {to}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to send email to {to}");
            throw;
        }
    }
}