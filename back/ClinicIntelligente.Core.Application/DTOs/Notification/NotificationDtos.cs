namespace ClinicIntelligente.Core.Application.DTOs.Notification;

public record NotificationDto(
    int Id,
    string Message,
    string Type,
    int? AppointmentId,
    bool IsRead,
    DateTime CreatedAt
);