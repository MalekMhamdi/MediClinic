namespace ClinicIntelligente.Core.Application.Interfaces.Repositories;

using ClinicIntelligente.Core.Domain.Entities;

public interface IConsultationRepository : IGenericRepository<Consultation>
{
    Task<Consultation?> GetWithPrescriptionsAndLabOrdersAsync(int id);
    Task<Consultation?> GetByAppointmentIdAsync(int appointmentId);
}