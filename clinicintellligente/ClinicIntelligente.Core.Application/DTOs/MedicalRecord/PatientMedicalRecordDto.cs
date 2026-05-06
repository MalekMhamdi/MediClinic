namespace ClinicIntelligente.Core.Application.DTOs.MedicalRecord;

using ClinicIntelligente.Core.Application.DTOs.Appointment;

public record PatientMedicalRecordDto(
    int PatientId,
    string FullName,
    string Cin,
    DateTime DateOfBirth,
    string? BloodGroup,
    string? Phone,
    List<AppointmentDto> Appointments
);