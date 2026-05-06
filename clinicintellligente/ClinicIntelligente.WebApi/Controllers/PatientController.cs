namespace ClinicIntelligente.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClinicIntelligente.Core.Application.DTOs.Patient;
using ClinicIntelligente.Core.Application.Interfaces.Services;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class PatientController : ControllerBase
{
    private readonly IPatientService _service;
    public PatientController(IPatientService service) => _service = service;

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    /// <summary>[Admin, Doctor] Tous les patients actifs</summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    /// <summary>[Admin, Doctor, Patient] Patient par ID</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin,Doctor,Patient")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null
            ? NotFound(new { message = $"Patient {id} not found." })
            : Ok(result);
    }

    /// <summary>[Admin, Doctor] Recherche par CIN</summary>
    [HttpGet("by-cin/{cin}")]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> GetByCin(string cin)
    {
        var result = await _service.GetByCinAsync(cin);
        return result is null
            ? NotFound(new { message = $"Patient CIN '{cin}' not found." })
            : Ok(result);
    }

    /// <summary>[Patient, Doctor, Admin] Dossier médical complet</summary>
    [HttpGet("{id:int}/medical-record")]
    [Authorize(Roles = "Patient,Doctor,Admin")]
    public async Task<IActionResult> GetMedicalRecord(int id)
    {
        var result = await _service.GetMedicalRecordAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>[Public] Inscription d'un nouveau patient — pas de token requis</summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>[Patient] Mettre à jour ses propres informations</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Patient,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePatientDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return NoContent();
    }

    /// <summary>[Admin] Désactiver un patient (soft-delete)</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}