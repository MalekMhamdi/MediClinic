namespace ClinicIntelligente.Core.Application.DTOs.Admin;

public record AdminStatsDto(
    int TotalPatients,
    int TotalDoctors,
    int TotalLabTechs,
    int TotalAppointments,
    int PendingAppointments,
    int CancelledAppointments,
    double CancellationRate,
    int TotalConsultations,
    int TotalLabOrders,
    List<MonthlyStatDto> AppointmentsByMonth,
    List<SpecialtyStatDto> AppointmentsBySpecialty
);

public record MonthlyStatDto(string Month, int Count);
public record SpecialtyStatDto(string Specialty, int Count);