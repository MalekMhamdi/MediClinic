namespace ClinicIntelligente.Core.Application.Interfaces.Services;

using ClinicIntelligente.Core.Application.DTOs.Patient;
using ClinicIntelligente.Core.Application.DTOs.MedicalRecord;

public interface IPatientService
{
    Task<IReadOnlyList<PatientDto>> GetAllAsync();
    Task<PatientDto?> GetByIdAsync(int id);
    Task<PatientDto?> GetByCinAsync(string cin);
    Task<PatientDto> CreateAsync(CreatePatientDto dto);
    Task UpdateAsync(int id, UpdatePatientDto dto);
    Task DeleteAsync(int id);
    Task<PatientMedicalRecordDto?> GetMedicalRecordAsync(int patientId);
}