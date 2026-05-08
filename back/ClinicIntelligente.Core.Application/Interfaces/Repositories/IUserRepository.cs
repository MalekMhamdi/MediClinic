using ClinicIntelligente.Core.Domain.Entities;

namespace ClinicIntelligente.Core.Application.Interfaces.Repositories;

public interface IUserRepository : IGenericRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email);
}


