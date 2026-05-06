// PROJECT: ClinicIntelligente.Infrastructure.Persistence
// PATH: Repositories/LabOrderRepository.cs
// REPLACE your existing file

using Microsoft.EntityFrameworkCore;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Core.Domain.Enums;
using ClinicIntelligente.Infrastructure.Persistence.Contexts;

namespace ClinicIntelligente.Infrastructure.Persistence.Repositories;

public class LabOrderRepository : GenericRepository<LabOrder>, ILabOrderRepository
{
    public LabOrderRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<LabOrder>> GetByConsultationIdAsync(int consultationId)
        => await _context.LabOrders
            .Include(lo => lo.LabResult)
                .ThenInclude(lr => lr!.Technicien)
            .Where(lo => lo.ConsultationId == consultationId)
            .OrderByDescending(lo => lo.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<LabOrder>> GetPendingAsync()
        => await _context.LabOrders
            .Include(lo => lo.Consultation)
                .ThenInclude(c => c!.Appointment)
                    .ThenInclude(a => a!.Patient)
            .Where(lo => lo.Status == LabStatus.Pending
                      || lo.Status == LabStatus.InProgress)
            .OrderBy(lo => lo.CreatedAt)   // oldest first → FIFO
            .ToListAsync();

    public async Task<LabOrder?> GetWithResultAsync(int labOrderId)
        => await _context.LabOrders
            .Include(lo => lo.LabResult)
                .ThenInclude(lr => lr!.Technicien)
            .FirstOrDefaultAsync(lo => lo.Id == labOrderId);

    public async Task<LabOrder?> GetWithFullDetailsAsync(int labOrderId)
        => await _context.LabOrders
            .Include(lo => lo.LabResult)
                .ThenInclude(lr => lr!.Technicien)
            .Include(lo => lo.Consultation)
                .ThenInclude(c => c!.Appointment)
                    .ThenInclude(a => a!.Patient)
            .Include(lo => lo.Consultation)
                .ThenInclude(c => c!.Appointment)
                    .ThenInclude(a => a!.Doctor)
                        .ThenInclude(d => d!.Specialty)
            .FirstOrDefaultAsync(lo => lo.Id == labOrderId);

    public async Task<IReadOnlyList<LabOrder>> GetAllWithStatusAsync(string? status = null)
    {
        var q = _context.LabOrders
            .Include(lo => lo.Consultation)
                .ThenInclude(c => c!.Appointment)
                    .ThenInclude(a => a!.Patient)
            .Include(lo => lo.Consultation)
                .ThenInclude(c => c!.Appointment)
                    .ThenInclude(a => a!.Doctor)
                        .ThenInclude(d => d!.Specialty)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<LabStatus>(status, ignoreCase: true, out var labStatus))
            q = q.Where(lo => lo.Status == labStatus);

        return await q.OrderByDescending(lo => lo.CreatedAt).ToListAsync();
    }
}