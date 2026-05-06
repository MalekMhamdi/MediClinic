namespace ClinicIntelligente.Core.Domain.Entities;

using ClinicIntelligente.Core.Domain.Common;
using ClinicIntelligente.Core.Domain.Enums;

public class Notification : AuditableBaseEntity
{
    /// <summary>Who receives this notification</summary>
    public int RecipientId { get; set; }

    /// <summary>Human-readable message shown in the UI</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>What triggered this notification</summary>
    public NotificationType Type { get; set; }

    /// <summary>Related appointment ID (for deep-link navigation)</summary>
    public int? AppointmentId { get; set; }

    /// <summary>False = unread (shown as badge in UI)</summary>
    public bool IsRead { get; set; } = false;

    // Navigation
    public User? Recipient { get; set; }
    public Appointment? Appointment { get; set; }
}