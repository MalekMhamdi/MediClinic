namespace ClinicIntelligente.Infrastructure.Persistence.Repositories;

using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

public class DoctorRepository : GenericRepository<Doctor>, IDoctorRepository
{
    public DoctorRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Doctor>> GetBySpecialtyAsync(int specialtyId)
        => await _context.Doctors
            .Include(d => d.Specialty)
            .Where(d => d.SpecialtyId == specialtyId && d.IsActive)
            .ToListAsync();

    public async Task<bool> ExistsByEmailAsync(string email)
        => await _context.Users.AnyAsync(u => u.Email == email);
}