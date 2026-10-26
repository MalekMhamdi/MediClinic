namespace ClinicIntelligente.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClinicIntelligente.Core.Application.DTOs.Review;
using ClinicIntelligente.Core.Application.Interfaces.Services;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class DoctorReviewController(IDoctorReviewService service) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>[Patient] Submit a rating for a completed appointment</summary>
    [HttpPost]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> Submit([FromBody] CreateReviewDto dto)
    {
        var review = await service.SubmitAsync(CurrentUserId, dto);
        return CreatedAtAction(nameof(GetDoctorRating), new { doctorId = review.DoctorId }, review);
    }

    /// <summary>[Patient] Check if patient can review a specific appointment</summary>
    [HttpGet("can-review/{appointmentId:int}")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> CanReview(int appointmentId)
        => Ok(await service.CanReviewAsync(CurrentUserId, appointmentId));

    /// <summary>[Any authenticated] Get rating and reviews for a doctor</summary>
    [HttpGet("doctor/{doctorId:int}")]
    [Authorize]
    public async Task<IActionResult> GetDoctorRating(int doctorId)
        => Ok(await service.GetDoctorRatingAsync(doctorId));

    /// <summary>[Patient] Get my own submitted reviews</summary>
    [HttpGet("my")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> GetMyReviews()
        => Ok(await service.GetMyReviewsAsync(CurrentUserId));
}
