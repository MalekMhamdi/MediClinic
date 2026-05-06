namespace ClinicIntelligente.Core.Domain.Entities;

using ClinicIntelligente.Core.Domain.Common; 


// Inherits User (TPT)
public class LabTechnicien : User
{
    public int SpecialtyId { get; set; }
    public string? Phone { get; set; }
    public string? Certification { get; set; }
    public string? LabSection { get; set; }

    public Specialty? Specialty { get; set; }
    public List<LabResult>? LabResults { get; set; } = new();
}