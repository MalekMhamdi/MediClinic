namespace ClinicIntelligente.Core.Application.Interfaces.Repositories;

using ClinicIntelligente.Core.Domain.Entities;

public interface IPrescriptionRepository : IGenericRepository<Prescription>
{
    Task<IReadOnlyList<Prescription>> GetByConsultationIdAsync(int consultationId);
}