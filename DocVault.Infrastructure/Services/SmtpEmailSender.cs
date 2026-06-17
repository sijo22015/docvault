using System.Net;
using System.Net.Mail;
using DocVault.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocVault.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration config, ILogger<SmtpEmailSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var host = _config["Email:SmtpHost"];
        var port = int.Parse(_config["Email:SmtpPort"] ?? "587");
        var user = _config["Email:SmtpUser"];
        var pass = _config["Email:SmtpPass"];
        var from = _config["Email:FromAddress"] ?? user;
        var fromName = _config["Email:FromName"] ?? "DocVault";

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            _logger.LogWarning("[EMAIL] SMTP not configured. To: {To} | Subject: {Subject}", to, subject);
            return;
        }

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(user, pass),
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        using var message = new MailMessage
        {
            From = new MailAddress(from!, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(to);

        await client.SendMailAsync(message, ct);
        _logger.LogInformation("[EMAIL] Sent to {To} | Subject: {Subject}", to, subject);
    }
}
