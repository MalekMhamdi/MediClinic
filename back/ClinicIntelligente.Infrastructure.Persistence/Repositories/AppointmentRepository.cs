namespace ClinicIntelligente.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Infrastructure.Persistence.Contexts;

public class AppointmentRepository : GenericRepository<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Appointment>> GetByPatientIdAsync(int patientId)
        => await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d!.Specialty)
            .Include(a => a.Consultation)
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.DateTime)
            .ToListAsync();

    public async Task<IReadOnlyList<Appointment>> GetByDoctorIdAsync(int doctorId)
        => await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d!.Specialty)
            .Include(a => a.Consultation)
            .Where(a => a.DoctorId == doctorId)
            .OrderByDescending(a => a.DateTime)
            .ToListAsync();

    public async Task<Appointment?> GetWithConsultationAsync(int appointmentId)
        => await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d!.Specialty)
            .Include(a => a.Consultation)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

    public async Task<IReadOnlyList<Appointment>> GetAllWithDetailsAsync()
        => await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d!.Specialty)
            .OrderByDescending(a => a.DateTime)
            .ToListAsync();
}