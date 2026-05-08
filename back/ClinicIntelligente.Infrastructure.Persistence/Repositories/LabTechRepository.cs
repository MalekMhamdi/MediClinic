using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace ClinicIntelligente.Infrastructure.Persistence.Repositories;

public class LabTechRepository
    : GenericRepository<LabTechnicien>, ILabTechRepository
{
    private readonly ApplicationDbContext _context;

    public LabTechRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<LabTechnicien?> GetByEmailAsync(string email)
    {
        return await _context.Set<LabTechnicien>()
            .FirstOrDefaultAsync(x => x.Email == email);
    }
}