namespace ClinicIntelligente.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClinicIntelligente.Core.Application.DTOs.LabOrder;
using ClinicIntelligente.Core.Application.Interfaces.Services;
using System.Security.Claims;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class LabResultController : ControllerBase
{
    private readonly ILabResultService _service;
    public LabResultController(ILabResultService service) => _service = service;

    /// <summary>[Doctor, LabTechnicien, Admin] Résultat par LabOrder ID</summary>
    [HttpGet("by-order/{labOrderId:int}")]
    [Authorize(Roles = "Doctor,LabTechnicien,Admin")]
    public async Task<IActionResult> GetByLabOrder(int labOrderId)
    {
        var result = await _service.GetByLabOrderIdAsync(labOrderId);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>[LabTechnicien, Admin] Résultats soumis par un technicien</summary>
    [HttpGet("by-technician/{technicienId:int}")]
    [Authorize(Roles = "LabTechnicien,Admin")]
    public async Task<IActionResult> GetByTechnician(int technicienId)
        => Ok(await _service.GetByTechnicienIdAsync(technicienId));

    /// <summary>[LabTechnicien] Soumettre un résultat — passe LabOrder à Done</summary>
    [HttpPost]
    [Authorize(Roles = "LabTechnicien")]
    public async Task<IActionResult> Create([FromBody] CreateLabResultDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(
            nameof(GetByLabOrder),
            new { labOrderId = created.LabOrderId },
            created);
    }

    /// <summary>[Doctor] Tous les résultats d'analyses liés aux patients du docteur</summary>
    [HttpGet("by-doctor/{doctorId:int}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> GetByDoctor(int doctorId)
        => Ok(await _service.GetByDoctorIdAsync(doctorId));

    /// <summary>[Doctor] Envoyer un commentaire au patient sur un résultat d'analyse</summary>
    [HttpPost("{labResultId:int}/doctor-comment")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> AddDoctorComment(int labResultId, [FromBody] DoctorCommentDto dto)
    {
        var doctorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _service.SendDoctorCommentAsync(labResultId, doctorId, dto.Comment);
        return Ok(new { message = "Comment sent to patient." });
    }
}