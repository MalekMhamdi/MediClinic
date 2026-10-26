namespace ClinicIntelligente.Core.Application.Interfaces.Services;

public interface IEmailService
{
    /// <summary>Send a single email. Silently swallows errors so a broken SMTP config never breaks the main flow.</summary>
    Task SendAsync(string toEmail, string toName, string subject, string htmlBody);
}
