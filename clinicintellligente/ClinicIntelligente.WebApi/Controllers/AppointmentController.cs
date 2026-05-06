namespace ClinicIntelligente.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ClinicIntelligente.Core.Application.DTOs.Appointment;
using ClinicIntelligente.Core.Application.Interfaces.Services;
using ClinicIntelligente.Core.Domain.Enums;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class AppointmentController(IAppointmentService service) : ControllerBase
{
    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── GET ALL (Admin only) ────────────────────────────────────────
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll()
        => Ok(await service.GetAllAsync());

    // ── GET BY ID ──────────────────────────────────────────────────
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    // ── GET BY PATIENT ─────────────────────────────────────────────
    [HttpGet("by-patient/{patientId:int}")]
    [Authorize(Roles = "Patient,Admin")]
    public async Task<IActionResult> GetByPatient(
        int patientId, [FromQuery] string? status = null)
        => Ok(await service.GetByPatientIdAsync(patientId, status));

    // ── GET BY DOCTOR ──────────────────────────────────────────────
    [HttpGet("by-doctor/{doctorId:int}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> GetByDoctor(
        int doctorId, [FromQuery] string? status = null)
        => Ok(await service.GetByDoctorIdAsync(doctorId, status));

    // ── GET AVAILABLE SLOTS ─────────────────────────────────────────
    [HttpGet("available-slots")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvailableSlots(
        [FromQuery] int doctorId, [FromQuery] DateTime date)
    {
        var slots = await service.GetAvailableSlotsAsync(doctorId, date);
        return Ok(slots);
    }

    // ── CREATE (Patient books) ─────────────────────────────────────
    [HttpPost]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
    {
        var result = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // ── ACCEPT (Doctor accepts) ────────────────────────────────────
    [HttpPatch("{id:int}/accept")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> Accept(int id)
    {
        await service.AcceptAsync(id, CurrentUserId);
        return NoContent();
    }

    // ── REFUSE (Doctor refuses) ────────────────────────────────────
    [HttpPatch("{id:int}/refuse")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> Refuse(int id)
    {
        await service.RefuseAsync(id, CurrentUserId);
        return NoContent();
    }

    // ── CANCEL (Patient cancels) ───────────────────────────────────
    [HttpPatch("{id:int}/cancel")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> Cancel(int id)
    {
        await service.CancelByPatientAsync(id, CurrentUserId);
        return NoContent();
    }

    // ── RESCHEDULE (Patient reschedules) ───────────────────────────
    [HttpPatch("{id:int}/reschedule")]
    [Authorize(Roles = "Patient")]
    public async Task<IActionResult> Reschedule(
        int id, [FromBody] RescheduleAppointmentDto dto)
    {
        await service.RescheduleAsync(id, CurrentUserId, dto.NewDateTime, dto.Reason);
        return NoContent();
    }

   

    // ── DELETE (Admin only) ────────────────────────────────────────
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }

    // ── TRIGGER REMINDERS ──────────────────────────────────────────
    [HttpPost("send-reminders")]
    [AllowAnonymous] // Usually triggered by a cron job or background service
    public async Task<IActionResult> SendReminders([FromQuery] DateTime? targetDate = null)
    {
        // Default to tomorrow if no date is provided
        var date = targetDate ?? DateTime.UtcNow.AddDays(1);
        await service.SendRemindersAsync(date);
        return Ok(new { message = $"Reminders sent for {date:yyyy-MM-dd}" });
    }
}