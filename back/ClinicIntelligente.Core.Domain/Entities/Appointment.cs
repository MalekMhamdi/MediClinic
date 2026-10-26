namespace ClinicIntelligente.Core.Domain.Entities;

using ClinicIntelligente.Core.Domain.Common;
using ClinicIntelligente.Core.Domain.Enums;

public class Appointment : AuditableBaseEntity
{
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime DateTime { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Reason { get; set; }
    public string? Notes { get; set; }
    public bool ReminderSent { get; set; } = false;

    public Patient? Patient { get; set; }
    public Doctor? Doctor { get; set; }
    public Consultation? Consultation { get; set; }
}