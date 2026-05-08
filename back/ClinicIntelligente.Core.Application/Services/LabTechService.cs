
using ClinicIntelligente.Core.Application.DTOs.LabTech;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Application.Interfaces.Services;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Core.Domain.Enums;

namespace ClinicIntelligente.Core.Application.Services;

public class LabTechService : ILabTechService
{
    private readonly ILabOrderRepository _labOrderRepo;
    private readonly ILabResultRepository _labResultRepo;
    private readonly INotificationService _notifications;

    public LabTechService(
        ILabOrderRepository labOrderRepo,
        ILabResultRepository labResultRepo,
        INotificationService notifications)
    {
        _labOrderRepo = labOrderRepo;
        _labResultRepo = labResultRepo;
        _notifications = notifications;
    }

    private PendingLabOrderDto MapToPendingDto(ClinicIntelligente.Core.Domain.Entities.LabOrder lo)
    {
        var doctor = lo.Consultation?.Appointment?.Doctor;
        return new PendingLabOrderDto(
            Id: lo.Id,
            ConsultationId: lo.ConsultationId,
            PatientFullName: GetPatientName(lo),
            PatientCin: GetPatientCin(lo),
            AnalysisType: lo.AnalysisType,
            Status: lo.Status.ToString(),
            DoctorNotes: lo.Notes,
            OrderedAt: lo.CreatedAt,
            DoctorFullName: doctor != null ? $"{doctor.FirstName} {doctor.LastName}" : "",
            DoctorSpecialty: doctor?.Specialty?.Name ?? ""
        );
    }

    public async Task<IReadOnlyList<PendingLabOrderDto>> GetPendingOrdersAsync()
    {
        var orders = await _labOrderRepo.GetPendingAsync();
        return orders.Select(MapToPendingDto).ToList();
    }

    public async Task<IReadOnlyList<PendingLabOrderDto>> GetAllOrdersAsync(string? status = null)
    {
        var orders = await _labOrderRepo.GetAllWithStatusAsync(status);
        return orders.Select(MapToPendingDto).ToList();
    }

    public async Task<LabOrderDetailDto> GetOrderDetailAsync(int labOrderId)
    {
        var lo = await _labOrderRepo.GetWithFullDetailsAsync(labOrderId)
            ?? throw new KeyNotFoundException($"Lab order {labOrderId} not found.");

        var patient = lo.Consultation?.Appointment?.Patient as Patient;
        var doctor = lo.Consultation?.Appointment?.Doctor;

        return new LabOrderDetailDto(
            Id: lo.Id,
            ConsultationId: lo.ConsultationId,
            PatientFullName: patient != null ? $"{patient.FirstName} {patient.LastName}" : "Unknown",
            PatientCin: patient?.Cin ?? "",
            PatientBloodGroup: patient?.BloodGroup ?? "",
            DoctorFullName: doctor != null ? $"{doctor.FirstName} {doctor.LastName}" : "Unknown",
            DoctorSpecialty: doctor?.Specialty?.Name ?? "",
            AnalysisType: lo.AnalysisType,
            Status: lo.Status.ToString(),
            DoctorNotes: lo.Notes,
            OrderedAt: lo.CreatedAt,
            Result: lo.LabResult is null ? null : new LabResultDetailDto(
                Id: lo.LabResult.Id,
                TechnicienFullName: lo.LabResult.Technicien != null
                    ? $"{lo.LabResult.Technicien.FirstName} {lo.LabResult.Technicien.LastName}"
                    : "Unknown",
                ResultPdfPath: lo.LabResult.ResultPdfPath,
                MedicalComment: lo.LabResult.MedicalComment,
                Status: lo.LabResult.Status.ToString(),
                SubmittedAt: lo.LabResult.CreatedAt
            )
        );
    }

    public async Task<IReadOnlyList<LabResultSummaryDto>> GetMyResultsAsync(int technicienId)
    {
        var results = await _labResultRepo.GetByTechnicienIdAsync(technicienId);
        return results.Select(MapToSummary).ToList();
    }

    public async Task<LabResultSummaryDto> SubmitResultAsync(int technicienId, SubmitLabResultDto dto)
    {
        var labOrder = await _labOrderRepo.GetWithFullDetailsAsync(dto.LabOrderId)
            ?? throw new KeyNotFoundException($"Lab order {dto.LabOrderId} not found.");

        if (labOrder.Status is LabStatus.Done or LabStatus.Reviewed)
            throw new InvalidOperationException("A result has already been submitted for this lab order.");

        var result = new LabResult
        {
            LabOrderId = dto.LabOrderId,
            TechnicienId = technicienId,
            ResultPdfPath = dto.ResultPdfPath,
            MedicalComment = dto.MedicalComment,
            Status = LabStatus.Done,
            CreatedAt = DateTime.UtcNow
        };

        await _labResultRepo.AddAsync(result);

        labOrder.Status = LabStatus.Done;
        labOrder.LastModifiedAt = DateTime.UtcNow;
        await _labOrderRepo.UpdateAsync(labOrder);

        // Notify both the doctor and the patient
        var appointment = labOrder.Consultation?.Appointment;
        if (appointment is not null)
        {
            await _notifications.NotifyLabResultAvailableAsync(
                labOrder.Id, appointment.DoctorId, appointment.PatientId, labOrder.AnalysisType);
        }

        return MapToSummary(result);
    }

    public async Task UpdateResultAsync(int labResultId, int technicienId, UpdateLabResultDto dto)
    {
        var result = await _labResultRepo.GetByIdAsync(labResultId)
            ?? throw new KeyNotFoundException($"Lab result {labResultId} not found.");

        if (result.TechnicienId != technicienId)
            throw new UnauthorizedAccessException("You can only update results you submitted.");

        result.MedicalComment = dto.Comment;
        result.ResultPdfPath = dto.ResultPdfPath ?? result.ResultPdfPath;
        result.LastModifiedAt = DateTime.UtcNow;
        await _labResultRepo.UpdateAsync(result);
    }

    private static string GetPatientName(LabOrder lo)
    {
        var p = lo.Consultation?.Appointment?.Patient as Patient;
        return p != null ? $"{p.FirstName} {p.LastName}" : "Unknown";
    }

    private static string GetPatientCin(LabOrder lo)
    {
        var p = lo.Consultation?.Appointment?.Patient as Patient;
        return p?.Cin ?? "";
    }

    private static LabResultSummaryDto MapToSummary(LabResult r) => new(
        Id: r.Id,
        LabOrderId: r.LabOrderId,
        AnalysisType: r.LabOrder?.AnalysisType ?? "",
        PatientFullName: r.LabOrder?.Consultation?.Appointment?.Patient is Patient p
                                ? $"{p.FirstName} {p.LastName}" : "Unknown",
        TechnicienFullName: r.Technicien != null
                                ? $"{r.Technicien.FirstName} {r.Technicien.LastName}" : "",
        Status: r.Status.ToString(),
        ResultPdfPath: r.ResultPdfPath,
        MedicalComment: r.MedicalComment,
        SubmittedAt: r.CreatedAt
    );
}