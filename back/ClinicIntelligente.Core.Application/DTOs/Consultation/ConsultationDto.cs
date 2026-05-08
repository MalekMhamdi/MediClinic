namespace ClinicIntelligente.Core.Application.DTOs.Consultation;

using ClinicIntelligente.Core.Application.DTOs.LabOrder;
using ClinicIntelligente.Core.Application.DTOs.Prescription;
using System.ComponentModel.DataAnnotations;

public record ConsultationDto(
    int Id,
    int AppointmentId,
    string? Diagnostic,
    string? Observations,
    string? Treatment,
    DateTime CreatedAt,
    List<PrescriptionDto> Prescriptions,
    List<LabOrderDto> LabOrders
);

public record CreateConsultationDto(
    [Required] int AppointmentId,
               string? Diagnostic,
               string? Observations,
               string? Treatment
);
