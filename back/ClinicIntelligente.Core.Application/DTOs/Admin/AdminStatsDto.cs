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
    List<SpecialtyStatDto> AppointmentsBySpecialty,
    List<DoctorStatDto> AppointmentsByDoctor
);

public record MonthlyStatDto(string Month, int Count);
public record SpecialtyStatDto(string Specialty, int Count);
public record DoctorStatDto(string Doctor, string Specialty, int Appointments, int Consultations);

public record AdminAppointmentDto(
    int Id,
    string PatientName,
    string DoctorName,
    string DoctorSpecialty,
    DateTime DateTime,
    string Status,
    string StatusLabel,
    bool HasConsultation,
    string? Reason
);