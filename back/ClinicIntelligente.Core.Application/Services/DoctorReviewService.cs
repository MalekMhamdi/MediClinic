namespace ClinicIntelligente.Core.Application.Services;

using ClinicIntelligente.Core.Application.DTOs.Review;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Application.Interfaces.Services;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Core.Domain.Enums;

public class DoctorReviewService(
    IDoctorReviewRepository reviewRepo,
    IAppointmentRepository  appointmentRepo
) : IDoctorReviewService
{
    public async Task<CanReviewDto> CanReviewAsync(int patientId, int appointmentId)
    {
        var appt = await appointmentRepo.GetWithConsultationAsync(appointmentId);

        if (appt is null)
            return new CanReviewDto(false, "Appointment not found.");

        if (appt.PatientId != patientId)
            return new CanReviewDto(false, "This appointment does not belong to you.");

        if (appt.Status != AppointmentStatus.Completed)
            return new CanReviewDto(false, "You can only rate a doctor after a completed consultation.");

        if (await reviewRepo.ExistsByAppointmentAsync(appointmentId))
            return new CanReviewDto(false, "You have already submitted a review for this appointment.");

        return new CanReviewDto(true, null);
    }

    public async Task<ReviewDto> SubmitAsync(int patientId, CreateReviewDto dto)
    {
        var check = await CanReviewAsync(patientId, dto.AppointmentId);
        if (!check.CanReview)
            throw new InvalidOperationException(check.Reason);

        var appt = await appointmentRepo.GetWithConsultationAsync(dto.AppointmentId)
            ?? throw new KeyNotFoundException("Appointment not found.");

        var review = new DoctorReview
        {
            DoctorId      = appt.DoctorId,
            PatientId     = patientId,
            AppointmentId = dto.AppointmentId,
            Rating        = dto.Rating,
            Comment       = dto.Comment?.Trim(),
            CreatedAt     = DateTime.UtcNow,
        };

        await reviewRepo.AddAsync(review);

        // reload with Patient
        var saved = await reviewRepo.GetByAppointmentIdAsync(dto.AppointmentId)
            ?? throw new Exception("Review not found after save.");

        return Map(saved);
    }

    public async Task<DoctorRatingDto> GetDoctorRatingAsync(int doctorId)
    {
        var reviews = await reviewRepo.GetByDoctorIdAsync(doctorId);
        var (avg, count) = await reviewRepo.GetStatsAsync(doctorId);

        return new DoctorRatingDto(
            DoctorId:      doctorId,
            AverageRating: avg,
            TotalReviews:  count,
            Reviews:       reviews.Select(Map).ToList()
        );
    }

    public async Task<IReadOnlyList<ReviewDto>> GetMyReviewsAsync(int patientId)
    {
        var reviews = await reviewRepo.GetByPatientIdAsync(patientId);
        return reviews.Select(Map).ToList();
    }

    private static ReviewDto Map(DoctorReview r) => new(
        Id:            r.Id,
        DoctorId:      r.DoctorId,
        PatientId:     r.PatientId,
        PatientName:   r.Patient is not null ? r.Patient.FullName : "Patient",
        AppointmentId: r.AppointmentId,
        Rating:        r.Rating,
        Comment:       r.Comment,
        CreatedAt:     r.CreatedAt
    );
}
