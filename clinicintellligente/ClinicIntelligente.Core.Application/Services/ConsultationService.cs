using AutoMapper;
using ClinicIntelligente.Core.Application.DTOs.Consultation;
using ClinicIntelligente.Core.Application.DTOs.Prescription;
using ClinicIntelligente.Core.Application.DTOs.LabOrder;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Application.Interfaces.Services;
using ClinicIntelligente.Core.Domain.Enums;

namespace ClinicIntelligente.Core.Application.Services;

public class ConsultationService : IConsultationService
{
    private readonly IConsultationRepository _consultationRepo;
    private readonly IPrescriptionRepository _prescriptionRepo;
    private readonly ILabOrderRepository _labOrderRepo;
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly INotificationService _notifications;
    private readonly IMapper _mapper;

    public ConsultationService(
        IConsultationRepository consultationRepo,
        IPrescriptionRepository prescriptionRepo,
        ILabOrderRepository labOrderRepo,
        IAppointmentRepository appointmentRepo,
        INotificationService notifications,
        IMapper mapper)
    {
        _consultationRepo = consultationRepo;
        _prescriptionRepo = prescriptionRepo;
        _labOrderRepo = labOrderRepo;
        _appointmentRepo = appointmentRepo;
        _notifications = notifications;
        _mapper = mapper;
    }

    public async Task<ConsultationDto?> GetByIdAsync(int id)
    {
        var c = await _consultationRepo.GetWithPrescriptionsAndLabOrdersAsync(id);
        return c is null ? null : _mapper.Map<ConsultationDto>(c);
    }

    public async Task<ConsultationDto?> GetByAppointmentIdAsync(int appointmentId)
    {
        var c = await _consultationRepo.GetByAppointmentIdAsync(appointmentId);
        return c is null ? null : _mapper.Map<ConsultationDto>(c);
    }

    // Doctor creates consultation → notify patient
    public async Task<ConsultationDto> CreateAsync(CreateConsultationDto dto)
    {
        var appointment = await _appointmentRepo.GetWithConsultationAsync(dto.AppointmentId)
            ?? throw new KeyNotFoundException($"Appointment {dto.AppointmentId} not found.");

        if (appointment.Status != AppointmentStatus.Confirmed)
            throw new InvalidOperationException("Consultation requires a confirmed appointment.");

        if (appointment.Consultation is not null)
            throw new InvalidOperationException("A consultation already exists for this appointment.");

        var consultation = _mapper.Map<Domain.Entities.Consultation>(dto);
        consultation.CreatedAt = DateTime.UtcNow;
        await _consultationRepo.AddAsync(consultation);

        appointment.Status = AppointmentStatus.Completed;
        appointment.LastModifiedAt = DateTime.UtcNow;
        await _appointmentRepo.UpdateAsync(appointment);

        // Notify patient
        var doctorName = appointment.Doctor != null
            ? $"{appointment.Doctor.FirstName} {appointment.Doctor.LastName}"
            : "votre médecin";

        await _notifications.NotifyConsultationCreatedAsync(
            consultation.Id, appointment.PatientId, doctorName);

        return _mapper.Map<ConsultationDto>(consultation);
    }

    // Doctor edits diagnostic/observations/treatment after creation
    public async Task UpdateAsync(int consultationId, UpdateConsultationDto dto)
    {
        var c = await _consultationRepo.GetByIdAsync(consultationId)
            ?? throw new KeyNotFoundException($"Consultation {consultationId} not found.");

        c.Diagnostic = dto.Diagnostic;
        c.Observations = dto.Observations;
        c.Treatment = dto.Treatment;
        c.LastModifiedAt = DateTime.UtcNow;
        await _consultationRepo.UpdateAsync(c);
    }

    // Add prescription to consultation
    public async Task AddPrescriptionAsync(CreatePrescriptionDto dto)
    {
        _ = await _consultationRepo.GetByIdAsync(dto.ConsultationId)
            ?? throw new KeyNotFoundException($"Consultation {dto.ConsultationId} not found.");

        var prescription = _mapper.Map<Domain.Entities.Prescription>(dto);
        prescription.CreatedAt = DateTime.UtcNow;
        await _prescriptionRepo.AddAsync(prescription);
    }

    // Prescribe a lab analysis
    public async Task AddLabOrderAsync(CreateLabOrderDto dto)
    {
        _ = await _consultationRepo.GetByIdAsync(dto.ConsultationId)
            ?? throw new KeyNotFoundException($"Consultation {dto.ConsultationId} not found.");

        var labOrder = _mapper.Map<Domain.Entities.LabOrder>(dto);
        labOrder.Status = LabStatus.Pending;
        labOrder.CreatedAt = DateTime.UtcNow;
        await _labOrderRepo.AddAsync(labOrder);
    }

    // Doctor adds medical comment + optionally attaches PDF to a lab result
    public async Task AddMedicalCommentAsync(int labOrderId, string comment, string? pdfPath = null)
    {
        var labOrder = await _labOrderRepo.GetWithResultAsync(labOrderId)
            ?? throw new KeyNotFoundException($"Lab order {labOrderId} not found.");

        if (labOrder.LabResult is null)
            throw new InvalidOperationException("No result available for this lab order yet.");

        labOrder.LabResult.MedicalComment = comment;

        if (!string.IsNullOrWhiteSpace(pdfPath))
            labOrder.LabResult.ResultPdfPath = pdfPath;

        labOrder.Status = LabStatus.Reviewed;
        labOrder.LastModifiedAt = DateTime.UtcNow;
        await _labOrderRepo.UpdateAsync(labOrder);
    }
}