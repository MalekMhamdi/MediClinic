namespace ClinicIntelligente.Core.Domain.Entities;

using ClinicIntelligente.Core.Domain.Common;

public class Consultation : AuditableBaseEntity
{
    public int AppointmentId { get; set; }
    public string? Diagnostic { get; set; }
    public string? Observations { get; set; }
    public string? Treatment { get; set; }

    public Appointment? Appointment { get; set; }
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public ICollection<LabOrder> LabOrders { get; set; } = new List<LabOrder>();
}