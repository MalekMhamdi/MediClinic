// ClinicIntelligente.Core.Application/Services/AppointmentService.cs
namespace ClinicIntelligente.Core.Application.Services;

using AutoMapper;
using ClinicIntelligente.Core.Application.DTOs.Appointment;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Application.Interfaces.Services;
using ClinicIntelligente.Core.Domain.Enums;

public class AppointmentService(
    IAppointmentRepository appointmentRepo,
    IPatientRepository patientRepo,
    IDoctorRepository doctorRepo,
    INotificationService notificationService,
    IMapper mapper
) : IAppointmentService
{
    public async Task<IReadOnlyList<AppointmentDto>> GetAllAsync()
        => mapper.Map<IReadOnlyList<AppointmentDto>>(await appointmentRepo.GetAllAsync());

    public async Task<AppointmentDto?> GetByIdAsync(int id)
    {
        var a = await appointmentRepo.GetByIdAsync(id);
        return a is null ? null : mapper.Map<AppointmentDto>(a);
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetByPatientIdAsync(
        int patientId, string? statusFilter = null)
    {
        var items = await appointmentRepo.GetByPatientIdAsync(patientId);

        if (!string.IsNullOrWhiteSpace(statusFilter) &&
            Enum.TryParse<AppointmentStatus>(statusFilter, ignoreCase: true, out var status))
            items = items.Where(a => a.Status == status).ToList();

        return mapper.Map<IReadOnlyList<AppointmentDto>>(items);
    }

    public async Task<IReadOnlyList<AppointmentDto>> GetByDoctorIdAsync(
        int doctorId, string? statusFilter = null)
    {
        var items = await appointmentRepo.GetByDoctorIdAsync(doctorId);

        if (!string.IsNullOrWhiteSpace(statusFilter) &&
            Enum.TryParse<AppointmentStatus>(statusFilter, ignoreCase: true, out var status))
            items = items.Where(a => a.Status == status).ToList();

        return mapper.Map<IReadOnlyList<AppointmentDto>>(items);
    }

    public async Task<IReadOnlyList<string>> GetAvailableSlotsAsync(int doctorId, DateTime date)
    {
        var doctorAppointments = await appointmentRepo.GetByDoctorIdAsync(doctorId);

        // Seuls Pending et Confirmed bloquent un créneau.
        // Completed/Cancelled/Refused libèrent le créneau pour de futures réservations.
        var takenSlots = doctorAppointments
            .Where(a =>
            {
                var local = ToLocal(a.DateTime);
                return local.Date == date.Date
                    && (a.Status == AppointmentStatus.Pending
                        || a.Status == AppointmentStatus.Confirmed);
            })
            .Select(a => ToLocal(a.DateTime)
                .ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var allSlots = new List<string>
        {
            "08:00 AM", "08:30 AM", "09:00 AM", "09:30 AM", "10:00 AM", "10:30 AM", "11:00 AM", "11:30 AM",
            "02:00 PM", "02:30 PM", "03:00 PM", "03:30 PM", "04:00 PM", "04:30 PM"
        };

        return allSlots.Where(s => !takenSlots.Contains(s)).ToList();
    }

    // EF Core reads SQL Server datetimes as Kind=Unspecified even though they were stored as UTC.
    // We must specify UTC kind before converting so that ToLocalTime() works correctly.
    private static DateTime ToLocal(DateTime dt)
    {
        if (dt.Kind == DateTimeKind.Unspecified)
            dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc);
        return dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;
    }

    public async Task<AppointmentDto> CreateAsync(CreateAppointmentDto dto)
    {
        var patient = await patientRepo.GetByIdAsync(dto.PatientId)
            ?? throw new KeyNotFoundException($"Patient {dto.PatientId} not found.");

        var doctor = await doctorRepo.GetByIdAsync(dto.DoctorId)
            ?? throw new KeyNotFoundException($"Doctor {dto.DoctorId} not found.");

        if (dto.DateTime <= DateTime.UtcNow)
            throw new InvalidOperationException("Appointment date must be in the future.");

        // Hard guard: reject if the doctor already has a Pending or Confirmed appointment at exactly this time
        var existingSlots = await appointmentRepo.GetByDoctorIdAsync(dto.DoctorId);
        var localRequested = ToLocal(dto.DateTime);
        var conflict = existingSlots.Any(a =>
            (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
            && Math.Abs((ToLocal(a.DateTime) - localRequested).TotalMinutes) < 1);
        if (conflict)
            throw new InvalidOperationException(
                $"The time slot {localRequested:hh:mm tt} on {localRequested:yyyy-MM-dd} is already taken for this doctor.");

        var appointment = mapper.Map<Domain.Entities.Appointment>(dto);
        appointment.Status = AppointmentStatus.Pending;
        appointment.CreatedAt = DateTime.UtcNow;
        await appointmentRepo.AddAsync(appointment);

        await notificationService.NotifyAppointmentBookedAsync(
            appointment.Id, patient.Id, doctor.Id, appointment.DateTime);

        return mapper.Map<AppointmentDto>(appointment);
    }

    public async Task AcceptAsync(int id, int doctorId)
    {
        var appt = await appointmentRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Appointment {id} not found.");

        if (appt.DoctorId != doctorId)
            throw new UnauthorizedAccessException("You can only accept your own appointments.");

        if (appt.Status != AppointmentStatus.Pending)
            throw new InvalidOperationException("Only pending appointments can be accepted.");

        var doctor = await doctorRepo.GetByIdAsync(doctorId)
            ?? throw new KeyNotFoundException($"Doctor {doctorId} not found.");

        appt.Status = AppointmentStatus.Confirmed;
        appt.LastModifiedAt = DateTime.UtcNow;
        await appointmentRepo.UpdateAsync(appt);

        await notificationService.NotifyAppointmentConfirmedAsync(
            appt.Id,
            appt.PatientId,
            $"Dr. {doctor.FirstName} {doctor.LastName}",
            appt.DateTime);
    }

    public async Task RefuseAsync(int id, int doctorId)
    {
        var appt = await appointmentRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Appointment {id} not found.");

        if (appt.DoctorId != doctorId)
            throw new UnauthorizedAccessException("You can only refuse your own appointments.");

        appt.Status = AppointmentStatus.Refused;
        appt.LastModifiedAt = DateTime.UtcNow;
        await appointmentRepo.UpdateAsync(appt);

        await notificationService.NotifyAppointmentCancelledAsync(
            appt.Id, appt.PatientId, doctorId, cancelledBy: "Doctor");
    }

    public async Task CancelByPatientAsync(int id, int patientId)
    {
        var appt = await appointmentRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Appointment {id} not found.");

        if (appt.PatientId != patientId)
            throw new UnauthorizedAccessException("You can only cancel your own appointments.");

        if (appt.Status == AppointmentStatus.Cancelled)
            throw new InvalidOperationException("Appointment is already cancelled.");

        appt.Status = AppointmentStatus.Cancelled;
        appt.LastModifiedAt = DateTime.UtcNow;
        await appointmentRepo.UpdateAsync(appt);

        await notificationService.NotifyAppointmentCancelledAsync(
            appt.Id, patientId, appt.DoctorId, cancelledBy: "Patient");
    }

    public async Task RescheduleAsync(int id, int patientId, DateTime newDateTime, string? reason)
    {
        var appt = await appointmentRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Appointment {id} not found.");

        if (appt.PatientId != patientId)
            throw new UnauthorizedAccessException("You can only reschedule your own appointments.");

        if (newDateTime <= DateTime.UtcNow)
            throw new InvalidOperationException("New date must be in the future.");

        appt.DateTime = newDateTime;
        appt.Reason = reason;
        appt.Status = AppointmentStatus.Pending;
        appt.LastModifiedAt = DateTime.UtcNow;
        await appointmentRepo.UpdateAsync(appt);

        await notificationService.NotifyAppointmentRescheduledAsync(
            appt.Id, patientId, appt.DoctorId, newDateTime);
    }

   

    public async Task DeleteAsync(int id)
    {
        var appt = await appointmentRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Appointment {id} not found.");
        await appointmentRepo.DeleteAsync(appt);
    }

    public async Task DeleteByPatientAsync(int id, int patientId)
    {
        var appt = await appointmentRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Appointment {id} not found.");
        if (appt.PatientId != patientId)
            throw new UnauthorizedAccessException("You can only delete your own appointments.");
        if (appt.Status == AppointmentStatus.Pending || appt.Status == AppointmentStatus.Confirmed)
            throw new InvalidOperationException("Cancel the appointment first before deleting it.");
        await appointmentRepo.DeleteAsync(appt);
    }

    public async Task DeleteByDoctorAsync(int id, int doctorId)
    {
        var appt = await appointmentRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Appointment {id} not found.");
        if (appt.DoctorId != doctorId)
            throw new UnauthorizedAccessException("You can only delete your own appointments.");
        if (appt.Status == AppointmentStatus.Pending || appt.Status == AppointmentStatus.Confirmed)
            throw new InvalidOperationException("Refuse the appointment first before deleting it.");
        await appointmentRepo.DeleteAsync(appt);
    }

    public async Task SendRemindersAsync(DateTime targetDate)
    {
        var target = targetDate.Date;

        var allAppointments = await appointmentRepo.GetAllAsync();
        var toRemind = allAppointments.Where(a =>
            ToLocal(a.DateTime).Date == target &&          // comparaison en heure locale
            (a.Status == AppointmentStatus.Confirmed ||
             a.Status == AppointmentStatus.Pending));

        foreach (var appt in toRemind)
        {
            await notificationService.NotifyAppointmentReminderAsync(
                appt.Id, appt.PatientId, appt.DateTime);
        }
    }
}