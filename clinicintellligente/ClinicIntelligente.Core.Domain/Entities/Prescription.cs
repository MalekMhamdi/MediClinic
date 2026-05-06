namespace ClinicIntelligente.Core.Domain.Entities;

using ClinicIntelligente.Core.Domain.Common;

public class Prescription : AuditableBaseEntity
{
    public int ConsultationId { get; set; }
    public string Medication { get; set; } = string.Empty;
    public string? Dosage { get; set; }
    public string? Duration { get; set; }
    public string? Instructions { get; set; }

    public Consultation? Consultation { get; set; }
}
