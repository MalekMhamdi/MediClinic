namespace ClinicIntelligente.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClinicIntelligente.Core.Application.DTOs.Specialty;
using ClinicIntelligente.Core.Application.Interfaces.Services;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class SpecialtyController : ControllerBase
{
    private readonly ISpecialtyService _service;
    public SpecialtyController(ISpecialtyService service) => _service = service;

    /// <summary>[Public] Toutes les spécialités</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    /// <summary>[Public] Spécialité par ID</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>[Admin] Créer une spécialité</summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateSpecialtyDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>[Admin] Modifier une spécialité</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateSpecialtyDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return NoContent();
    }

    /// <summary>[Admin] Supprimer une spécialité</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}