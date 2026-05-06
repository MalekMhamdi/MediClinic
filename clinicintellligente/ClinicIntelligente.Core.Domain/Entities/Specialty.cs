namespace ClinicIntelligente.Core.Domain.Entities;

using ClinicIntelligente.Core.Domain.Common;

public class Specialty : AuditableBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public List<Doctor> Doctors { get; set; } = [];
    public List<LabTechnicien> LabTechniciens { get; set; } = [];
}