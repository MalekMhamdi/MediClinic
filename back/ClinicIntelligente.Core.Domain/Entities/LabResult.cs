namespace ClinicIntelligente.Core.Domain.Entities;

using ClinicIntelligente.Core.Domain.Common;
using ClinicIntelligente.Core.Domain.Enums;

public class LabResult : AuditableBaseEntity
{
    public int LabOrderId { get; set; }
    public int TechnicienId { get; set; }
    public string? ResultPdfPath { get; set; }
    public string? MedicalComment { get; set; }
    public LabStatus Status { get; set; } = LabStatus.Done;

    public LabOrder? LabOrder { get; set; }
    public LabTechnicien? Technicien { get; set; }
}