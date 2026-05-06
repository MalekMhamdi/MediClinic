

using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Core.Domain.Enums;

namespace ClinicIntelligente.Core.Application.Interfaces.Repositories;

public interface ILabOrderRepository : IGenericRepository<LabOrder>
{
    Task<IReadOnlyList<LabOrder>> GetByConsultationIdAsync(int consultationId);
    Task<IReadOnlyList<LabOrder>> GetPendingAsync();
    Task<LabOrder?> GetWithResultAsync(int labOrderId);

    /// <summary>
    /// Loads LabOrder + LabResult + Technicien + Consultation + Appointment + Patient + Doctor + Specialty
    /// Used for the detail view shown to the lab technician
    /// </summary>
    Task<LabOrder?> GetWithFullDetailsAsync(int labOrderId);
    Task<IReadOnlyList<LabOrder>> GetAllWithStatusAsync(string? status = null);
}