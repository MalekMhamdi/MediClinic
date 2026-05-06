namespace ClinicIntelligente.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Infrastructure.Persistence.Contexts;

public class PrescriptionRepository : GenericRepository<Prescription>, IPrescriptionRepository
{
    public PrescriptionRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Prescription>> GetByConsultationIdAsync(int consultationId)
        => await _context.Prescriptions
            .Where(p => p.ConsultationId == consultationId)
            .ToListAsync();
}