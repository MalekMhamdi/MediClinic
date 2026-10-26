namespace ClinicIntelligente.Core.Application.Services;

using ClinicIntelligente.Core.Application.DTOs.Notification;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Application.Interfaces.Services;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Core.Domain.Enums;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;

    public NotificationService(INotificationRepository repo)
        => _repo = repo;

    // ── READ ──────────────────────────────────────────────────────

    public async Task<IReadOnlyList<NotificationDto>> GetMyNotificationsAsync(int userId)
    {
        var notifications = await _repo.GetByRecipientIdAsync(userId);
        return notifications
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationDto(
                n.Id,
                n.Message,
                n.Type.ToString(),
                n.AppointmentId,
                n.IsRead,
                n.CreatedAt))
            .ToList();
    }

    public async Task<int> GetUnreadCountAsync(int userId)
        => await _repo.GetUnreadCountAsync(userId);

    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        var n = await _repo.GetByIdAsync(notificationId)
            ?? throw new KeyNotFoundException($"Notification {notificationId} not found.");

        if (n.RecipientId != userId)
            throw new UnauthorizedAccessException("This notification does not belong to you.");

        n.IsRead = true;
        n.LastModifiedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(n);
    }

    public async Task MarkAllAsReadAsync(int userId)
        => await _repo.MarkAllAsReadAsync(userId);

    public async Task DeleteAsync(int notificationId, int userId)
        => await _repo.DeleteAsync(notificationId, userId);

    // ── TRIGGERS ─────────────────────────────────────────────────

    public async Task NotifyAppointmentBookedAsync(
        int appointmentId, int patientId, int doctorId, DateTime dateTime)
    {
        await CreateAsync(new Notification
        {
            RecipientId = doctorId,
            Message = $"Nouveau rendez-vous demandé pour le {dateTime:dd/MM/yyyy à HH:mm}.",
            Type = NotificationType.AppointmentBooked,
            AppointmentId = appointmentId,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task NotifyAppointmentConfirmedAsync(
        int appointmentId, int patientId, string doctorName, DateTime dateTime)
    {
        // Patient is notified
        await CreateAsync(new Notification
        {
            RecipientId = patientId,
            Message = $"Votre rendez-vous du {dateTime:dd/MM/yyyy à HH:mm} a été confirmé par Dr. {doctorName}.",
            Type = NotificationType.AppointmentConfirmed,
            AppointmentId = appointmentId,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task NotifyAppointmentCancelledAsync(
        int appointmentId, int patientId, int doctorId, string cancelledBy)
    {
        // Both are notified
        await CreateAsync(new Notification
        {
            RecipientId = patientId,
            Message = $"Votre rendez-vous a été annulé par {cancelledBy}.",
            Type = NotificationType.AppointmentCancelled,
            AppointmentId = appointmentId,
            CreatedAt = DateTime.UtcNow
        });

        await CreateAsync(new Notification
        {
            RecipientId = doctorId,
            Message = $"Un rendez-vous a été annulé par {cancelledBy}.",
            Type = NotificationType.AppointmentCancelled,
            AppointmentId = appointmentId,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task NotifyAppointmentRescheduledAsync(
        int appointmentId, int patientId, int doctorId, DateTime newDateTime)
    {
        // Doctor is notified
        await CreateAsync(new Notification
        {
            RecipientId = doctorId,
            Message = $"Un rendez-vous a été reporté au {newDateTime:dd/MM/yyyy à HH:mm}.",
            Type = NotificationType.AppointmentRescheduled,
            AppointmentId = appointmentId,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task NotifyLabResultAvailableAsync(
        int labOrderId, int doctorId, int patientId, string analysisType)
    {
        // Notify the doctor
        await CreateAsync(new Notification
        {
            RecipientId = doctorId,
            Message = $"Lab result available for analysis: {analysisType}.",
            Type = NotificationType.LabResultAvailable,
            AppointmentId = null,
            CreatedAt = DateTime.UtcNow
        });
        // Notify the patient
        await CreateAsync(new Notification
        {
            RecipientId = patientId,
            Message = $"Your lab result for \"{analysisType}\" is ready. You can view it in your medical record.",
            Type = NotificationType.LabResultAvailable,
            AppointmentId = null,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task NotifyConsultationCreatedAsync(
        int consultationId, int patientId, string doctorName)
    {
        await CreateAsync(new Notification
        {
            RecipientId = patientId,
            Message = $"Votre consultation avec Dr. {doctorName} a été enregistrée. Consultez votre dossier médical.",
            Type = NotificationType.ConsultationCreated,
            AppointmentId = null,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task NotifyAppointmentReminderAsync(
        int appointmentId, int patientId, DateTime dateTime)
    {
        await CreateAsync(new Notification
        {
            RecipientId = patientId,
            Message = $"Rappel : Vous avez un rendez-vous demain à {dateTime:HH:mm}.",
            Type = NotificationType.SystemAlert, // Or a specific Reminder type if it exists
            AppointmentId = appointmentId,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task NotifyDoctorCommentAsync(
        int patientId, int doctorId, string analysisType, string comment)
    {
        await CreateAsync(new Notification
        {
            RecipientId = patientId,
            Message = $"Commentaire du médecin sur votre analyse ({analysisType}) : {comment}",
            Type = NotificationType.LabResultAvailable,
            AppointmentId = null,
            CreatedAt = DateTime.UtcNow
        });
    }

    // ── PRIVATE ───────────────────────────────────────────────────

    private async Task CreateAsync(Notification n)
        => await _repo.AddAsync(n);
}