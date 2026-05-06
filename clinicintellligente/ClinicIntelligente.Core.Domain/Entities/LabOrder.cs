namespace ClinicIntelligente.Core.Domain.Entities;

using ClinicIntelligente.Core.Domain.Common;
using ClinicIntelligente.Core.Domain.Enums;

public class LabOrder : AuditableBaseEntity
{
    public int ConsultationId { get; set; }
    public string AnalysisType { get; set; } = string.Empty;
    public LabStatus Status { get; set; } = LabStatus.Pending;
    public string? Notes { get; set; }

    public Consultation? Consultation { get; set; }
    public LabResult? LabResult { get; set; }
}