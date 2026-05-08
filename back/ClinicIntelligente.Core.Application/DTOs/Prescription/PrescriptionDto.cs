using System.ComponentModel.DataAnnotations;

namespace ClinicIntelligente.Core.Application.DTOs.Prescription;

public record PrescriptionDto(
    int Id,
    int ConsultationId,
    string Medication,
    string? Dosage,
    string? Duration,
    string? Instructions
);

public record CreatePrescriptionDto(
    [Required] int ConsultationId,      
    [Required] string Medication,
               string? Dosage,
               string? Duration,
               string? Instructions
);