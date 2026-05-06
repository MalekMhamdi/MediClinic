// PROJECT: ClinicIntelligente.Core.Application
// PATH: DTOs/LabTech/LabTechDtos.cs

using System.ComponentModel.DataAnnotations;
using ClinicIntelligente.Core.Domain.Enums;

namespace ClinicIntelligente.Core.Application.DTOs.LabTech;

/// <summary>
/// A lab order waiting to be processed by the technician.
/// Returned by GET /api/v1/labtech/orders/pending
/// </summary>
public record PendingLabOrderDto(
    int Id,
    int ConsultationId,
    string PatientFullName,
    string PatientCin,
    string AnalysisType,
    string Status,
    string? DoctorNotes,
    DateTime OrderedAt,
    string DoctorFullName = "",
    string DoctorSpecialty = ""
);

/// <summary>
/// Summary of a lab result after it has been submitted.
/// </summary>
public record LabResultSummaryDto(
    int Id,
    int LabOrderId,
    string AnalysisType,
    string PatientFullName,
    string TechnicienFullName,
    string Status,
    string? ResultPdfPath,
    string? MedicalComment,
    DateTime SubmittedAt
);

/// <summary>
/// Payload to submit a new lab result for a given lab order.
/// POST /api/v1/labtech/results
/// </summary>
public record SubmitLabResultDto(
    [Required] int LabOrderId,
    string? ResultPdfPath,
    string? MedicalComment
);

/// <summary>
/// Payload to update comment or PDF on an existing result.
/// PATCH /api/v1/labtech/results/{id}
/// </summary>
public record UpdateLabResultDto(
    [Required] string Comment,
    string? ResultPdfPath
);

/// <summary>
/// Detailed view of a single lab order with its result (if any).
/// GET /api/v1/labtech/orders/{id}
/// </summary>
public record LabOrderDetailDto(
    int Id,
    int ConsultationId,
    string PatientFullName,
    string PatientCin,
    string PatientBloodGroup,
    string DoctorFullName,
    string DoctorSpecialty,
    string AnalysisType,
    string Status,
    string? DoctorNotes,
    DateTime OrderedAt,
    LabResultDetailDto? Result
);

/// <summary>
/// Embedded result inside LabOrderDetailDto.
/// </summary>
public record LabResultDetailDto(
    int Id,
    string TechnicienFullName,
    string? ResultPdfPath,
    string? MedicalComment,
    string Status,
    DateTime SubmittedAt
);