namespace ClinicIntelligente.Core.Domain.Enums;

public enum NotificationType
{
    AppointmentBooked = 1,   // → Doctor receives this
    AppointmentConfirmed = 2,   // → Patient receives this
    AppointmentCancelled = 3,   // → Both receive this
    AppointmentRescheduled = 4,  // → Doctor receives this
    LabResultAvailable = 5,   // → Doctor receives this
    ConsultationCreated = 6,   // → Patient receives this
    SystemAlert = 7,           // → Generic system notification / reminder
}