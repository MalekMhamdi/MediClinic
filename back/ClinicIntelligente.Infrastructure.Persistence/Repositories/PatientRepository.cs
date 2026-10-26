namespace ClinicIntelligente.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Infrastructure.Persistence.Contexts;

public class PatientRepository : GenericRepository<Patient>, IPatientRepository
{
    public PatientRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Patient?> GetByCinAsync(string cin)
        => await _context.Patients
            .FirstOrDefaultAsync(p => p.Cin == cin);

    public async Task<bool> ExistsByCinAsync(string cin)
        => await _context.Patients
            .AnyAsync(p => p.Cin == cin);

    public async Task<bool> ExistsByEmailAsync(string email)
        => await _context.Users
            .AnyAsync(u => u.Email == email);

    public async Task<Patient?> GetWithAppointmentsAsync(int patientId)
        => await _context.Patients
            .Include(p => p.Appointments)
                .ThenInclude(a => a.Doctor)
                    .ThenInclude(d => d!.Specialty)
            .FirstOrDefaultAsync(p => p.Id == patientId);

    public async Task<Patient?> GetWithFullMedicalRecordAsync(int patientId)
        => await _context.Patients
            .IgnoreQueryFilters()   // bypass IsActive filter so doctors can read any patient record
            .Include(p => p.Appointments)
                .ThenInclude(a => a.Doctor)
                    .ThenInclude(d => d!.Specialty)
            .Include(p => p.Appointments)
                .ThenInclude(a => a.Consultation)
                    .ThenInclude(c => c!.Prescriptions)
            .Include(p => p.Appointments)
                .ThenInclude(a => a.Consultation)
                    .ThenInclude(c => c!.LabOrders)
                        .ThenInclude(lo => lo.LabResult)
                            .ThenInclude(lr => lr!.Technicien)
            .FirstOrDefaultAsync(p => p.Id == patientId);
}