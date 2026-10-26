namespace ClinicIntelligente.Core.Application.DTOs.Review;

using System.ComponentModel.DataAnnotations;

public record CreateReviewDto(
    [Required] int AppointmentId,
    [Range(1, 5)] int Rating,
    [MaxLength(1000)] string? Comment
);

public record ReviewDto(
    int Id,
    int DoctorId,
    int PatientId,
    string PatientName,
    int AppointmentId,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);

public record DoctorRatingDto(
    int DoctorId,
    double AverageRating,
    int TotalReviews,
    IReadOnlyList<ReviewDto> Reviews
);

public record CanReviewDto(
    bool CanReview,
    string? Reason   // null when CanReview is true
);
