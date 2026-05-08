namespace ClinicIntelligente.Core.Application.DTOs.Appointment;

using System.ComponentModel.DataAnnotations;

public record RescheduleAppointmentDto(
    [Required] DateTime NewDateTime,
               string? Reason
);