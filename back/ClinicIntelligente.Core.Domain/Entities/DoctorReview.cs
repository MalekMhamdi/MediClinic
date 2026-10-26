namespace ClinicIntelligente.Core.Domain.Entities;

using ClinicIntelligente.Core.Domain.Common;

public class DoctorReview : AuditableBaseEntity
{
    public int DoctorId      { get; set; }
    public int PatientId     { get; set; }
    public int AppointmentId { get; set; }   // one review per completed appointment
    public int Rating        { get; set; }   // 1 – 5
    public string? Comment   { get; set; }

    public Doctor?      Doctor      { get; set; }
    public Patient?     Patient     { get; set; }
    public Appointment? Appointment { get; set; }
}
