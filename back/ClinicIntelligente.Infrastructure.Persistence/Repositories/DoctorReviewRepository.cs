namespace ClinicIntelligente.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Infrastructure.Persistence.Contexts;

public class DoctorReviewRepository : IDoctorReviewRepository
{
    private readonly ApplicationDbContext _context;
    public DoctorReviewRepository(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<DoctorReview>> GetByDoctorIdAsync(int doctorId)
        => await _context.DoctorReviews
            .Include(r => r.Patient)
            .Where(r => r.DoctorId == doctorId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<IReadOnlyList<DoctorReview>> GetByPatientIdAsync(int patientId)
        => await _context.DoctorReviews
            .Include(r => r.Doctor)
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<DoctorReview?> GetByAppointmentIdAsync(int appointmentId)
        => await _context.DoctorReviews
            .Include(r => r.Patient)
            .FirstOrDefaultAsync(r => r.AppointmentId == appointmentId);

    public async Task<bool> ExistsByAppointmentAsync(int appointmentId)
        => await _context.DoctorReviews.AnyAsync(r => r.AppointmentId == appointmentId);

    public async Task AddAsync(DoctorReview review)
    {
        await _context.DoctorReviews.AddAsync(review);
        await _context.SaveChangesAsync();
    }

    public async Task<(double Average, int Count)> GetStatsAsync(int doctorId)
    {
        var reviews = await _context.DoctorReviews
            .Where(r => r.DoctorId == doctorId)
            .Select(r => r.Rating)
            .ToListAsync();

        if (reviews.Count == 0) return (0, 0);
        return (Math.Round(reviews.Average(), 1), reviews.Count);
    }
}
