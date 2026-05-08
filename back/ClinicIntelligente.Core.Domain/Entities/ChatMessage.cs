namespace ClinicIntelligente.Core.Domain.Entities;

using ClinicIntelligente.Core.Domain.Common;

public class ChatMessage : AuditableBaseEntity
{
    public int SenderId   { get; set; }
    public int ReceiverId { get; set; }

    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;

    /// <summary>Soft-delete: sender clicked "supprimer"</summary>
    public bool IsDeletedBySender   { get; set; } = false;

    /// <summary>Soft-delete: receiver clicked "supprimer"</summary>
    public bool IsDeletedByReceiver { get; set; } = false;

    // Navigation
    public User Sender   { get; set; } = null!;
    public User Receiver { get; set; } = null!;
}
