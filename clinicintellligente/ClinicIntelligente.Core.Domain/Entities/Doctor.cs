namespace ClinicIntelligente.Core.Domain.Entities;

using ClinicIntelligente.Core.Domain.Common; 


// Inherits User (TPT)
public class Doctor : User
{
    public int SpecialtyId { get; set; }
    public string? Phone { get; set; }
    public string? LicenseNumber { get; set; }
    public string? ImageUrl { get; set; }
    public string? OfficeLocation { get; set; }

    public Specialty? Specialty { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}