namespace ClinicIntelligente.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClinicIntelligente.Core.Application.DTOs.Auth;
using ClinicIntelligente.Core.Application.Interfaces.Services;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    /// <summary>
    /// [Public] Inscription patient.
    /// Body : { firstName, lastName, email, password, cin,
    ///          dateOfBirth, bloodGroup?, phone?, address? }
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterPatient([FromBody] RegisterPatientDto dto)
    {
        var result = await _auth.RegisterPatientAsync(dto);
        return StatusCode(201, result);
    }

    /// <summary>
    /// [Admin] Créer un compte Médecin.
    /// Body : { firstName, lastName, email, password,
    ///          specialtyId, phone?, licenseNumber?, officeLocation? }
    /// </summary>
    [HttpPost("register/doctor")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegisterDoctor([FromBody] RegisterDoctorDto dto)
    {
        var result = await _auth.RegisterDoctorAsync(dto);
        return StatusCode(201, result);
    }

    /// <summary>
    /// [Admin] Créer un compte LabTechnicien.
    /// Body : { firstName, lastName, email, password,
    ///          specialtyId, phone?, certification?, labSection? }
    /// </summary>
    [HttpPost("register/labtech")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegisterLabTech([FromBody] RegisterLabTechDto dto)
    {
        var result = await _auth.RegisterLabTechAsync(dto);
        return StatusCode(201, result);
    }

    /// <summary>
    /// [Public] Connexion — retourne le JWT token.
    /// Copiez la valeur de "token" dans le bouton Authorize → Bearer {token}
    /// Body : { email, password }
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
        => Ok(await _auth.LoginAsync(dto));

    /// <summary>[Authorize] Mettre à jour son profil</summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        await _auth.UpdateProfileAsync(GetUserId(), dto);
        return NoContent();
    }

    /// <summary>[Authorize] Changer son mot de passe</summary>
    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        await _auth.ChangePasswordAsync(GetUserId(), dto);
        return NoContent();
    }

    /// <summary>[Authorize] Mon profil connecté</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var me = await _auth.GetCurrentUserAsync(GetUserId());
        return me is null ? NotFound() : Ok(me);
    }
}