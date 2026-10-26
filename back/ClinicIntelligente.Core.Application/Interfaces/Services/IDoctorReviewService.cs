namespace ClinicIntelligente.Core.Application.Interfaces.Services;

using ClinicIntelligente.Core.Application.DTOs.Review;

public interface IDoctorReviewService
{
    Task<ReviewDto>     SubmitAsync(int patientId, CreateReviewDto dto);
    Task<DoctorRatingDto> GetDoctorRatingAsync(int doctorId);
    Task<IReadOnlyList<ReviewDto>> GetMyReviewsAsync(int patientId);
    Task<CanReviewDto>  CanReviewAsync(int patientId, int appointmentId);
}
