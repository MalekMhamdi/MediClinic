namespace ClinicIntelligente.Core.Application.DTOs.MedicalRecord;

// ── Nested DTOs for full medical record ──────────────────────────────────

public record MedicalPrescriptionDto(
    int Id, string Medication, string? Dosage, string? Duration, string? Instructions);

public record MedicalLabResultDto(
    int Id, string TechnicienName, string? MedicalComment, string? ResultPdfPath,
    string Status, DateTime CreatedAt);

public record MedicalLabOrderDto(
    int Id, string AnalysisType, string Status, string StatusLabel,
    string? Notes, MedicalLabResultDto? Result);

public record MedicalConsultationDto(
    int Id, string? Diagnostic, string? Observations, string? Treatment, DateTime CreatedAt,
    List<MedicalPrescriptionDto> Prescriptions, List<MedicalLabOrderDto> LabOrders);

public record MedicalRecordAppointmentDto(
    int Id, string DoctorFullName, string DoctorSpecialty, DateTime DateTime,
    string Status, string StatusLabel, string? Reason,
    bool HasConsultation, MedicalConsultationDto? Consultation);

// ── Root DTO ─────────────────────────────────────────────────────────────

public record PatientMedicalRecordDto(
    int PatientId, string FullName, string Cin, DateTime DateOfBirth,
    string? BloodGroup, string? Phone,
    List<MedicalRecordAppointmentDto> Appointments);
