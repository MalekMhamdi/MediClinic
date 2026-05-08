namespace ClinicIntelligente.Core.Application.Interfaces.Repositories;

using ClinicIntelligente.Core.Domain.Entities;

public interface IPatientRepository : IGenericRepository<Patient>
{
    Task<Patient?> GetByCinAsync(string cin);
    Task<bool> ExistsByCinAsync(string cin);
    Task<bool> ExistsByEmailAsync(string email);
    Task<Patient?> GetWithFullMedicalRecordAsync(int patientId);
    Task<Patient?> GetWithAppointmentsAsync(int patientId);
}