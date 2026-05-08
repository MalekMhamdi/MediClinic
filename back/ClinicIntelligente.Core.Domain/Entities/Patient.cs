namespace ClinicIntelligente.Core.Domain.Entities;

using ClinicIntelligente.Core.Domain.Common;

// Inherits User (TPT — Table Per Type strategy in EF Core)
// Patient table only stores Patient-specific columns.
// Shared columns (Id, FirstName, Email…) stay in Users table.
public class Patient : User
{
    public string Cin { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string? BloodGroup { get; set; }
    public string? Phone { get; set; }
    public string? ImageUrl { get; set; }
    public string? Address { get; set; }

    // Emergency contact
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactRelation { get; set; }
    public string? EmergencyContactPhone { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}