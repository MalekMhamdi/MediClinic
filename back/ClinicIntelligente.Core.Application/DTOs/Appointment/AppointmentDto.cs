namespace ClinicIntelligente.Core.Application.DTOs.Appointment;

using ClinicIntelligente.Core.Domain.Enums;
using System.ComponentModel.DataAnnotations;

public class AppointmentDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string PatientFullName { get; set; } = null!;
    public int DoctorId { get; set; }
    public string DoctorFullName { get; set; } = null!;
    public string DoctorSpecialty { get; set; } = null!;
    public DateTime DateTime { get; set; }
    public AppointmentStatus Status { get; set; }
    public string StatusLabel { get; set; } = null!;
    public string? Reason { get; set; }
    public bool HasConsultation { get; set; }
}

public record CreateAppointmentDto(
    [Required] int PatientId,
    [Required] int DoctorId,
    [Required] DateTime DateTime,
               string? Reason
);

public record UpdateAppointmentStatusDto(
    [Required] AppointmentStatus Status
);