
using ClinicIntelligente.Core.Application.DTOs.Consultation;
using ClinicIntelligente.Core.Application.DTOs.Prescription;
using ClinicIntelligente.Core.Application.DTOs.LabOrder;

namespace ClinicIntelligente.Core.Application.Interfaces.Services;

public interface IConsultationService
{
    Task<ConsultationDto?> GetByIdAsync(int id);
    Task<ConsultationDto?> GetByAppointmentIdAsync(int appointmentId);
    Task<ConsultationDto> CreateAsync(CreateConsultationDto dto);
    Task UpdateAsync(int consultationId, UpdateConsultationDto dto);
    Task AddPrescriptionAsync(CreatePrescriptionDto dto);
    Task AddLabOrderAsync(CreateLabOrderDto dto);
    Task AddMedicalCommentAsync(int labOrderId, string comment, string? pdfPath = null);
}