// ClinicIntelligente.Core.Application/Services/LabResultService.cs
namespace ClinicIntelligente.Core.Application.Services;

using AutoMapper;
using ClinicIntelligente.Core.Application.DTOs.LabOrder;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Application.Interfaces.Services;
using ClinicIntelligente.Core.Domain.Enums;

public class LabResultService : ILabResultService
{
    private readonly ILabResultRepository _labResultRepo;
    private readonly ILabOrderRepository _labOrderRepo;
    private readonly IConsultationRepository _consultationRepo;
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;

    public LabResultService(
        ILabResultRepository labResultRepo,
        ILabOrderRepository labOrderRepo,
        IConsultationRepository consultationRepo,
        IAppointmentRepository appointmentRepo,
        INotificationService notificationService,
        IMapper mapper)
    {
        _labResultRepo = labResultRepo;
        _labOrderRepo = labOrderRepo;
        _consultationRepo = consultationRepo;
        _appointmentRepo = appointmentRepo;
        _notificationService = notificationService;
        _mapper = mapper;
    }

    public async Task<LabResultDto?> GetByLabOrderIdAsync(int labOrderId)
    {
        var r = await _labResultRepo.GetByLabOrderIdAsync(labOrderId);
        return r is null ? null : _mapper.Map<LabResultDto>(r);
    }

    public async Task<IReadOnlyList<LabResultDto>> GetByTechnicienIdAsync(int technicienId)
        => _mapper.Map<IReadOnlyList<LabResultDto>>(
            await _labResultRepo.GetByTechnicienIdAsync(technicienId));

    public async Task<LabResultDto> CreateAsync(CreateLabResultDto dto)
    {
        var labOrder = await _labOrderRepo.GetByIdAsync(dto.LabOrderId)
            ?? throw new KeyNotFoundException($"LabOrder {dto.LabOrderId} not found.");

        if (labOrder.Status is LabStatus.Done or LabStatus.Reviewed)
            throw new InvalidOperationException(
                "Un résultat existe déjà pour cette analyse.");

        var result = _mapper.Map<Domain.Entities.LabResult>(dto);
        result.Status = LabStatus.Done;
        result.CreatedAt = DateTime.UtcNow;
        await _labResultRepo.AddAsync(result);

        labOrder.Status = LabStatus.Done;
        labOrder.LastModifiedAt = DateTime.UtcNow;
        await _labOrderRepo.UpdateAsync(labOrder);

        var consultation = await _consultationRepo.GetByIdAsync(labOrder.ConsultationId);
        if (consultation is not null)
        {
            var appointment = await _appointmentRepo.GetByIdAsync(consultation.AppointmentId);
            if (appointment is not null)
            {
                // ── replaced SendAsync with the correct interface method ──
                await _notificationService.NotifyLabResultAvailableAsync(
                    labOrderId: labOrder.Id,
                    doctorId: appointment.DoctorId,
                    patientId: appointment.PatientId,
                    analysisType: labOrder.AnalysisType
                );
            }
        }

        return _mapper.Map<LabResultDto>(result);
    }

    public async Task<IReadOnlyList<DoctorLabResultDto>> GetByDoctorIdAsync(int doctorId)
    {
        var results = await _labResultRepo.GetByDoctorIdAsync(doctorId);
        return results.Select(r =>
        {
            var lo = r.LabOrder;
            var appt = lo?.Consultation?.Appointment;
            var patient = appt?.Patient;
            return new DoctorLabResultDto(
                Id: r.Id,
                LabOrderId: r.LabOrderId,
                AnalysisType: lo?.AnalysisType ?? "—",
                PatientId: patient?.Id ?? 0,
                PatientName: patient is not null ? $"{patient.FirstName} {patient.LastName}" : "—",
                TechnicienName: r.Technicien is not null ? $"{r.Technicien.FirstName} {r.Technicien.LastName}" : "—",
                LabComment: r.MedicalComment,
                Status: r.Status,
                StatusLabel: r.Status.ToString(),
                CreatedAt: r.CreatedAt
            );
        }).ToList();
    }

    public async Task DeleteAsync(int id)
    {
        var result = await _labResultRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"LabResult {id} not found.");

        // Reset the lab order status back to Pending so it can be re-submitted
        var labOrder = await _labOrderRepo.GetByIdAsync(result.LabOrderId);
        if (labOrder is not null)
        {
            labOrder.Status = LabStatus.Pending;
            labOrder.LastModifiedAt = DateTime.UtcNow;
            await _labOrderRepo.UpdateAsync(labOrder);
        }

        await _labResultRepo.DeleteAsync(result);
    }

    public async Task SendDoctorCommentAsync(int labResultId, int doctorId, string comment)
    {
        var result = await _labResultRepo.GetByIdAsync(labResultId)
            ?? throw new KeyNotFoundException($"LabResult {labResultId} not found.");

        var labOrder = await _labOrderRepo.GetByIdAsync(result.LabOrderId)
            ?? throw new KeyNotFoundException("LabOrder not found.");

        var consultation = await _consultationRepo.GetByIdAsync(labOrder.ConsultationId)
            ?? throw new KeyNotFoundException("Consultation not found.");

        var appointment = await _appointmentRepo.GetByIdAsync(consultation.AppointmentId)
            ?? throw new KeyNotFoundException("Appointment not found.");

        await _notificationService.NotifyDoctorCommentAsync(
            patientId: appointment.PatientId,
            doctorId: doctorId,
            analysisType: labOrder.AnalysisType,
            comment: comment
        );
    }
}