namespace ClinicIntelligente.Core.Application.Interfaces.Repositories;

using ClinicIntelligente.Core.Domain.Entities;

public interface ILabTechRepository : IGenericRepository<LabTechnicien>
{
    Task<LabTechnicien?> GetByEmailAsync(string email);
}