namespace ClinicIntelligente.Core.Application.Interfaces.Repositories;

using ClinicIntelligente.Core.Domain.Entities;

public interface IAppointmentRepository : IGenericRepository<Appointment>
{
    Task<IReadOnlyList<Appointment>> GetByPatientIdAsync(int patientId);
    Task<IReadOnlyList<Appointment>> GetByDoctorIdAsync(int doctorId);
    Task<Appointment?> GetWithConsultationAsync(int appointmentId);
    Task<IReadOnlyList<Appointment>> GetAllWithDetailsAsync();
}