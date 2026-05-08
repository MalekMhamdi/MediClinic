namespace ClinicIntelligente.Core.Application.Interfaces.Repositories;

using ClinicIntelligente.Core.Domain.Entities;

public interface ISpecialtyRepository : IGenericRepository<Specialty>
{
    Task<bool> ExistsByNameAsync(string name);
}