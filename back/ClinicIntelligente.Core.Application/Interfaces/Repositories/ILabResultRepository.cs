

using ClinicIntelligente.Core.Domain.Entities;

namespace ClinicIntelligente.Core.Application.Interfaces.Repositories;

public interface ILabResultRepository : IGenericRepository<LabResult>
{
    Task<IReadOnlyList<LabResult>> GetByTechnicienIdAsync(int technicienId);
    Task<LabResult?> GetByLabOrderIdAsync(int labOrderId);
    Task<IReadOnlyList<LabResult>> GetByDoctorIdAsync(int doctorId);
}