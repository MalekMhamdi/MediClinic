

using Microsoft.EntityFrameworkCore;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Infrastructure.Persistence.Contexts;

namespace ClinicIntelligente.Infrastructure.Persistence.Repositories;

public class LabResultRepository : GenericRepository<LabResult>, ILabResultRepository
{
    public LabResultRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<LabResult>> GetByTechnicienIdAsync(int technicienId)
        => await _context.LabResults
            .Include(r => r.Technicien)
            .Include(r => r.LabOrder)
                .ThenInclude(lo => lo!.Consultation)
                    .ThenInclude(c => c!.Appointment)
                        .ThenInclude(a => a!.Patient)
            .Where(r => r.TechnicienId == technicienId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<LabResult?> GetByLabOrderIdAsync(int labOrderId)
        => await _context.LabResults
            .Include(r => r.Technicien)
            .FirstOrDefaultAsync(r => r.LabOrderId == labOrderId);

    public async Task<IReadOnlyList<LabResult>> GetByDoctorIdAsync(int doctorId)
        => await _context.LabResults
            .Include(r => r.Technicien)
            .Include(r => r.LabOrder)
                .ThenInclude(lo => lo!.Consultation)
                    .ThenInclude(c => c!.Appointment)
                        .ThenInclude(a => a!.Patient)
            .Where(r => r.LabOrder!.Consultation!.Appointment!.DoctorId == doctorId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
}