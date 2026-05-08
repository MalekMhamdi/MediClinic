namespace ClinicIntelligente.Core.Application.Interfaces.Services;

using ClinicIntelligente.Core.Application.DTOs.Notification;

public interface INotificationService
{
    /// <summary>Get all notifications for the authenticated user</summary>
    Task<IReadOnlyList<NotificationDto>> GetMyNotificationsAsync(int userId);

    /// <summary>Number of unread notifications (for badge in UI)</summary>
    Task<int> GetUnreadCountAsync(int userId);

    /// <summary>Mark a single notification as read</summary>
    Task MarkAsReadAsync(int notificationId, int userId);

    /// <summary>Mark all notifications as read</summary>
    Task MarkAllAsReadAsync(int userId);

    /// <summary>Delete a notification (only owner can delete)</summary>
    Task DeleteAsync(int notificationId, int userId);

    // ── Internal triggers (called by other services) ──────────────

    /// <summary>
    /// Called when a patient books an appointment.
    /// Creates a notification for the Doctor.
    /// </summary>
    Task NotifyAppointmentBookedAsync(int appointmentId, int patientId, int doctorId, DateTime dateTime);

    /// <summary>
    /// Called when a doctor accepts an appointment.
    /// Creates a notification for the Patient.
    /// </summary>
    Task NotifyAppointmentConfirmedAsync(int appointmentId, int patientId, string doctorName, DateTime dateTime);

    /// <summary>
    /// Called when a doctor or patient cancels an appointment.
    /// Creates notifications for both parties.
    /// </summary>
    Task NotifyAppointmentCancelledAsync(int appointmentId, int patientId, int doctorId, string cancelledBy);

    /// <summary>
    /// Called when a patient reschedules.
    /// Creates a notification for the Doctor.
    /// </summary>
    Task NotifyAppointmentRescheduledAsync(int appointmentId, int patientId, int doctorId, DateTime newDateTime);

    /// <summary>
    /// Called when a lab result is submitted.
    /// Creates a notification for the Doctor who ordered it.
    /// </summary>
    Task NotifyLabResultAvailableAsync(int labOrderId, int doctorId, int patientId, string analysisType);

    /// <summary>
    /// Called when a consultation is created.
    /// Creates a notification for the Patient.
    /// </summary>
    Task NotifyConsultationCreatedAsync(int consultationId, int patientId, string doctorName);

    /// <summary>
    /// Called to remind a patient of an upcoming appointment.
    /// </summary>
    Task NotifyAppointmentReminderAsync(int appointmentId, int patientId, DateTime dateTime);

    /// <summary>
    /// Called when a doctor adds a comment on a lab result.
    /// Creates a notification for the Patient with the doctor's comment.
    /// </summary>
    Task NotifyDoctorCommentAsync(int patientId, int doctorId, string analysisType, string comment);
}
