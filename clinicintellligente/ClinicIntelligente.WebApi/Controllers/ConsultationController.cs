namespace ClinicIntelligente.WebApi.Controllers;

using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClinicIntelligente.Core.Application.DTOs.Consultation;
using ClinicIntelligente.Core.Application.DTOs.Prescription;
using ClinicIntelligente.Core.Application.DTOs.LabOrder;
using ClinicIntelligente.Core.Application.Interfaces.Services;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ConsultationController : ControllerBase
{
    private readonly IConsultationService _service;
    public ConsultationController(IConsultationService service) => _service = service;

    /// <summary>[Doctor, Admin, Patient] Consultation par ID</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Doctor,Admin,Patient")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>[Doctor, Admin, Patient] Consultation par rendez-vous</summary>
    [HttpGet("by-appointment/{appointmentId:int}")]
    [Authorize(Roles = "Doctor,Admin,Patient")]
    public async Task<IActionResult> GetByAppointment(int appointmentId)
    {
        var result = await _service.GetByAppointmentIdAsync(appointmentId);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>[Doctor] Créer une consultation — le RDV doit être Confirmed</summary>
    [HttpPost]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> Create([FromBody] CreateConsultationDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>[Doctor] Mettre à jour diagnostic et observations</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateConsultationDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return NoContent();
    }

    /// <summary>[Doctor] Ajouter une ordonnance à une consultation</summary>
    [HttpPost("{consultationId:int}/prescriptions")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> AddPrescription(
        int consultationId, [FromBody] CreatePrescriptionDto dto)
    {
        if (consultationId != dto.ConsultationId)
            return BadRequest("ConsultationId mismatch.");
        await _service.AddPrescriptionAsync(dto);
        return NoContent();
    }

    /// <summary>[Doctor] Prescrire une analyse depuis une consultation</summary>
    [HttpPost("{consultationId:int}/lab-orders")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> AddLabOrder(
        int consultationId, [FromBody] CreateLabOrderDto dto)
    {
        if (consultationId != dto.ConsultationId)
            return BadRequest("ConsultationId mismatch.");
        await _service.AddLabOrderAsync(dto);
        return NoContent();
    }

    /// <summary>
    /// [Doctor] Ajouter un commentaire médical sur un résultat d'analyse.
    /// Passe le LabOrder au statut Reviewed.
    /// Body : { "comment": "...", "pdfPath": "/uploads/..." }
    /// </summary>
    [HttpPatch("lab-orders/{labOrderId:int}/comment")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> AddMedicalComment(
        int labOrderId, [FromBody] AddCommentDto dto)
    {
        await _service.AddMedicalCommentAsync(labOrderId, dto.Comment, dto.PdfPath);
        return NoContent();
    }
}

public record AddCommentDto(
    [Required] string Comment,
    string? PdfPath
);