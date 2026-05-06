namespace ClinicIntelligente.Core.Application.DTOs.LabOrder;

using ClinicIntelligente.Core.Domain.Enums;
using System.ComponentModel.DataAnnotations;

public record LabOrderDto(
    int Id,
    int ConsultationId,
    string AnalysisType,
    LabStatus Status,
    string StatusLabel,
    string? Notes,
    LabResultDto? Result
);

public record CreateLabOrderDto(
   [Required] int ConsultationId, 
   [Required] string AnalysisType,
               string? Notes
);

public record LabResultDto(
    int Id,
    int LabOrderId,
    int TechnicienId,
    string TechnicienName,
    string? ResultPdfPath,
    string? MedicalComment,
    LabStatus Status,
    string StatusLabel,
    DateTime CreatedAt
);

public record CreateLabResultDto(
    [Required] int LabOrderId,
    [Required] int TechnicienId,
               string? ResultPdfPath,
               string? MedicalComment
);

public record DoctorLabResultDto(
    int Id,
    int LabOrderId,
    string AnalysisType,
    int PatientId,
    string PatientName,
    string TechnicienName,
    string? LabComment,
    LabStatus Status,
    string StatusLabel,
    DateTime CreatedAt
);

public record DoctorCommentDto(
    [Required] string Comment
);