

using ClinicIntelligente.Core.Application.DTOs.Appointment;
using ClinicIntelligente.Core.Domain.Enums;

namespace ClinicIntelligente.Core.Application.Interfaces.Services;

public interface IAppointmentService
{
    Task<IReadOnlyList<AppointmentDto>> GetAllAsync();
    Task<IReadOnlyList<AppointmentDto>> GetByPatientIdAsync(int patientId, string? statusFilter = null);
    Task<IReadOnlyList<AppointmentDto>> GetByDoctorIdAsync(int doctorId, string? statusFilter = null);
    Task<AppointmentDto?> GetByIdAsync(int id);
    Task<IReadOnlyList<string>> GetAvailableSlotsAsync(int doctorId, DateTime date, int? patientId = null);
    Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto);

    // Doctor actions
    Task AcceptAsync(int id, int doctorId);
    Task RefuseAsync(int id, int doctorId);

    // Patient actions
    Task CancelByPatientAsync(int id, int patientId);
    Task RescheduleAsync(int id, int patientId, DateTime newDateTime, string? reason);

    // Admin/generic
    Task DeleteAsync(int id);
    Task DeleteByPatientAsync(int id, int patientId);
    Task DeleteByDoctorAsync(int id, int doctorId);
    Task SendRemindersAsync(DateTime targetDate);
}