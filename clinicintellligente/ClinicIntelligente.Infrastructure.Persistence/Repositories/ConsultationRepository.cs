namespace ClinicIntelligente.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Infrastructure.Persistence.Contexts;

public class ConsultationRepository : GenericRepository<Consultation>, IConsultationRepository
{
    public ConsultationRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Consultation?> GetWithPrescriptionsAndLabOrdersAsync(int id)
        => await _context.Consultations
            .Include(c => c.Prescriptions)
            .Include(c => c.LabOrders)
                .ThenInclude(lo => lo.LabResult)
                    .ThenInclude(lr => lr!.Technicien)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Consultation?> GetByAppointmentIdAsync(int appointmentId)
        => await _context.Consultations
            .Include(c => c.Prescriptions)
            .Include(c => c.LabOrders).ThenInclude(lo => lo.LabResult)
            .FirstOrDefaultAsync(c => c.AppointmentId == appointmentId);
}