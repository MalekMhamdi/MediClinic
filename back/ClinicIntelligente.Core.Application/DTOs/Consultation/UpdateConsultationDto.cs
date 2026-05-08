namespace ClinicIntelligente.Core.Application.DTOs.Consultation;

public record UpdateConsultationDto(
    string? Diagnostic,
    string? Observations,
    string? Treatment
);