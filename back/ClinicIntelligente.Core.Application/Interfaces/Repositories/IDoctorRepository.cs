namespace ClinicIntelligente.Core.Application.Interfaces.Repositories;

using ClinicIntelligente.Core.Domain.Entities;
using System.Numerics;

public interface IDoctorRepository : IGenericRepository<Doctor>
{
    Task<IReadOnlyList<Doctor>> GetBySpecialtyAsync(int specialtyId);
    Task<bool> ExistsByEmailAsync(string email);
}