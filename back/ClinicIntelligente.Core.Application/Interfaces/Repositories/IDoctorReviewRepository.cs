namespace ClinicIntelligente.Core.Application.Interfaces.Repositories;

using ClinicIntelligente.Core.Domain.Entities;

public interface IDoctorReviewRepository
{
    Task<IReadOnlyList<DoctorReview>> GetByDoctorIdAsync(int doctorId);
    Task<IReadOnlyList<DoctorReview>> GetByPatientIdAsync(int patientId);
    Task<DoctorReview?> GetByAppointmentIdAsync(int appointmentId);
    Task<bool> ExistsByAppointmentAsync(int appointmentId);
    Task AddAsync(DoctorReview review);
    Task<(double Average, int Count)> GetStatsAsync(int doctorId);
}
