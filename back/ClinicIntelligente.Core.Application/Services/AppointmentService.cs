// ClinicIntelligente.Core.Application/Services/AppointmentService.cs
namespace ClinicIntelligente.Core.Application.Services;

using AutoMapper;
using ClinicIntelligente.Core.Application.DTOs.Appointment;
using ClinicIntelligente.Core.Application.Email;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Application.Interfaces.Services;
using ClinicIntelligente.Core.Domain.Enums;

public class AppointmentService(
    IAppointmentRepository appointmentRepo,
    IPatientRepository patientRepo,
    IDoctorRepository doctorRepo,
    INotificationService notificationService,
    IEmailService emailService,
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

    public async Task<IReadOnlyList<string>> GetAvailableSlotsAsync(
        int doctorId, DateTime date, int? patientId = null)
    {
        var allSlots = new List<string>
        {
            "08:00 AM", "08:30 AM", "09:00 AM", "09:30 AM", "10:00 AM", "10:30 AM", "11:00 AM", "11:30 AM",
            "02:00 PM", "02:30 PM", "03:00 PM", "03:30 PM", "04:00 PM", "04:30 PM"
        };

        // 1. Block slots already taken by ANY patient for this doctor (Pending/Confirmed)
        var doctorAppts = await appointmentRepo.GetByDoctorIdAsync(doctorId);
        var takenByDoctor = doctorAppts
            .Where(a => ToLocal(a.DateTime).Date == date.Date
                     && (a.Status == AppointmentStatus.Pending
                      || a.Status == AppointmentStatus.Confirmed))
            .Select(a => ToLocal(a.DateTime)
                .ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 2. Also block slots where THIS patient already has any active appointment
        //    (even with a different doctor) — a patient can't be in two places at once
        var takenByPatient = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (patientId.HasValue)
        {
            var patientAppts = await appointmentRepo.GetByPatientIdAsync(patientId.Value);
            takenByPatient = patientAppts
                .Where(a => ToLocal(a.DateTime).Date == date.Date
                         && (a.Status == AppointmentStatus.Pending
                          || a.Status == AppointmentStatus.Confirmed))
                .Select(a => ToLocal(a.DateTime)
                    .ToString("hh:mm tt", System.Globalization.CultureInfo.InvariantCulture))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        return allSlots
            .Where(s => !takenByDoctor.Contains(s) && !takenByPatient.Contains(s))
            .ToList();
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

        var localRequested = ToLocal(dto.DateTime);

        // Guard 0 — clinic is closed on Sundays
        if (localRequested.DayOfWeek == DayOfWeek.Sunday)
            throw new InvalidOperationException("The clinic is closed on Sundays. Please choose another day.");

        // Guard 1 — doctor already has Pending/Confirmed at this exact time
        var doctorSlots = await appointmentRepo.GetByDoctorIdAsync(dto.DoctorId);
        var doctorConflict = doctorSlots.Any(a =>
            (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
            && Math.Abs((ToLocal(a.DateTime) - localRequested).TotalMinutes) < 1);
        if (doctorConflict)
            throw new InvalidOperationException(
                $"The slot {localRequested:hh:mm tt} on {localRequested:yyyy-MM-dd} is already booked for this doctor.");

        // Guard 2 — same patient already has an active appointment at this exact time (any doctor)
        var patientSlots = await appointmentRepo.GetByPatientIdAsync(dto.PatientId);
        var patientConflict = patientSlots.Any(a =>
            (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed)
            && Math.Abs((ToLocal(a.DateTime) - localRequested).TotalMinutes) < 1);
        if (patientConflict)
            throw new InvalidOperationException(
                $"You already have an appointment at {localRequested:hh:mm tt} on {localRequested:yyyy-MM-dd}. Please choose a different time slot.");

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

        // Send immediate reminder if appointment is within 24h and not already sent
        var localAppt  = ToLocal(appt.DateTime);
        var hoursUntil = (localAppt - DateTime.Now).TotalHours;
        if (hoursUntil is > 0 and <= 24 && !appt.ReminderSent)
        {
            appt.ReminderSent = true;
            await appointmentRepo.UpdateAsync(appt);

            await notificationService.NotifyAppointmentReminderAsync(
                appt.Id, appt.PatientId, appt.DateTime);

            var patient = await patientRepo.GetByIdAsync(appt.PatientId);
            if (patient is not null)
            {
                var patientName = $"{patient.FirstName} {patient.LastName}";
                var doctorName  = $"Dr. {doctor.FirstName} {doctor.LastName}";

                await emailService.SendAsync(
                    toEmail:  patient.Email,
                    toName:   patientName,
                    subject:  $"[MediClinic] Rappel : Rendez-vous aujourd'hui à {localAppt:HH:mm}",
                    htmlBody: EmailTemplates.Reminder(patientName, localAppt, doctorName, ""));
            }
        }
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
        appt.ReminderSent = false;
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

        var all = await appointmentRepo.GetAllWithDetailsAsync();
        var toRemind = all.Where(a =>
            ToLocal(a.DateTime).Date == target
            && !a.ReminderSent
            && (a.Status == AppointmentStatus.Confirmed
             || a.Status == AppointmentStatus.Pending));

        foreach (var appt in toRemind)
        {
            appt.ReminderSent = true;
            await appointmentRepo.UpdateAsync(appt);

            await notificationService.NotifyAppointmentReminderAsync(
                appt.Id, appt.PatientId, appt.DateTime);

            if (appt.Patient is not null)
            {
                var patientName = $"{appt.Patient.FirstName} {appt.Patient.LastName}";
                var doctorName  = appt.Doctor is not null
                    ? $"{appt.Doctor.FirstName} {appt.Doctor.LastName}"
                    : "votre médecin";
                var specialty   = appt.Doctor?.Specialty?.Name ?? "";
                var localDt     = ToLocal(appt.DateTime);

                await emailService.SendAsync(
                    toEmail:  appt.Patient.Email,
                    toName:   patientName,
                    subject:  $"[MediClinic] Rappel : Rendez-vous demain à {localDt:HH:mm}",
                    htmlBody: EmailTemplates.Reminder(patientName, localDt, doctorName, specialty));
            }
        }
    }
}