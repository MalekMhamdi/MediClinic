namespace ClinicIntelligente.Infrastructure.Persistence.Services;

using System.Globalization;
using ClinicIntelligente.Core.Application.Interfaces.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

// EmailService: sends emails via SMTP (configured in appsettings.json → Email section).
// Set Email:Enabled = "false" to disable all sending (useful in dev without SMTP credentials).

public class EmailService : IEmailService
{
    private readonly IConfiguration _cfg;
    private readonly ILogger<EmailService> _log;

    public EmailService(IConfiguration cfg, ILogger<EmailService> log)
    {
        _cfg = cfg;
        _log = log;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var section = _cfg.GetSection("Email");
        var host     = section["SmtpHost"]     ?? "";
        var portStr  = section["SmtpPort"]     ?? "587";
        var user     = section["Username"]     ?? "";
        var pass     = section["Password"]     ?? "";
        var fromAddr = section["FromAddress"]  ?? user;
        var fromName = section["FromName"]     ?? "MediClinic";
        var enabled  = section["Enabled"]      ?? "true";

        if (enabled.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            _log.LogInformation("[Email DISABLED] Would send '{Subject}' to {To}", subject, toEmail);
            return;
        }

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(user))
        {
            _log.LogWarning("[Email] SMTP not configured — skipping email to {To}", toEmail);
            return;
        }

        _log.LogInformation("[Email] Attempting to send '{Subject}' to {To} via {Host}:{Port}",
            subject, toEmail, host, portStr);

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromAddr));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var smtp = new SmtpClient();

            // Bypass Windows enterprise SSL certificate inspection issues
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            var port = int.Parse(portStr, CultureInfo.InvariantCulture);
            var secureOption = port == 465 ? SecureSocketOptions.SslOnConnect
                                           : SecureSocketOptions.StartTls;

            await smtp.ConnectAsync(host, port, secureOption);
            _log.LogInformation("[Email] Connected to {Host}:{Port}", host, port);

            await smtp.AuthenticateAsync(user, pass);
            _log.LogInformation("[Email] Authenticated as {User}", user);

            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            _log.LogInformation("[Email] Successfully sent '{Subject}' to {To}", subject, toEmail);
        }
        catch (Exception ex)
        {
            // Log full exception detail to help diagnose SMTP issues
            _log.LogError(ex,
                "[Email] FAILED to send '{Subject}' to {To} — {ErrorType}: {ErrorMessage}",
                subject, toEmail, ex.GetType().Name, ex.Message);
        }
    }
}
