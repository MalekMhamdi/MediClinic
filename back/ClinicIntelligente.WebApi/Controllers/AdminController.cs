namespace ClinicIntelligente.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClinicIntelligente.Core.Application.DTOs.Admin;
using ClinicIntelligente.Core.Application.Interfaces.Services;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _service;
    public AdminController(IAdminService service) => _service = service;

    /// <summary>[Admin] Statistiques globales de la clinique</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
        => Ok(await _service.GetStatsAsync());

    /// <summary>[Admin] All appointments with optional status/specialty filter</summary>
    [HttpGet("appointments")]
    public async Task<IActionResult> GetAllAppointments(
        [FromQuery] string? status = null,
        [FromQuery] string? specialty = null)
        => Ok(await _service.GetAllAppointmentsAsync(status, specialty));

    /// <summary>[Admin] Tous les utilisateurs avec leurs rôles</summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
        => Ok(await _service.GetAllUsersAsync());

    /// <summary>[Admin] Activer / Désactiver un utilisateur</summary>
    [HttpPatch("users/{id:int}/toggle")]
    public async Task<IActionResult> ToggleUser(int id)
    {
        await _service.ToggleUserActiveAsync(id);
        return NoContent();
    }

    /// <summary>[Admin] Changer le rôle d'un utilisateur</summary>
    [HttpPatch("users/{id:int}/role")]
    public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleDto dto)
    {
        await _service.ChangeUserRoleAsync(id, dto.Role);
        return NoContent();
    }

    /// <summary>[Admin] Créer un nouvel utilisateur</summary>
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateAdminUserDto dto)
    {
        var created = await _service.CreateUserAsync(dto);
        return CreatedAtAction(nameof(GetAllUsers), new { id = created.Id }, created);
    }

    /// <summary>[Admin] Modifier un utilisateur</summary>
    [HttpPut("users/{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateAdminUserDto dto)
    {
        await _service.UpdateUserAsync(id, dto);
        return NoContent();
    }

    /// <summary>[Admin] Supprimer un utilisateur</summary>
    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        await _service.DeleteUserAsync(id);
        return NoContent();
    }
}