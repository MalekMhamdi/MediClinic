namespace ClinicIntelligente.WebApi.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClinicIntelligente.Core.Application.DTOs.Ai;
using ClinicIntelligente.Core.Application.Interfaces.Services;

[ApiController]
[Route("api/v1/ai")]
[Authorize(Roles = "Patient")]   // seul le Patient utilise l'IA d'orientation
[Produces("application/json")]
public class AiOrientationController : ControllerBase
{
    private readonly IAiOrientationService _service;
    public AiOrientationController(IAiOrientationService service) => _service = service;

    /// <summary>
    /// Étape 1 — Créer une session IA. Retourne un session_id.
    /// Utilisez ce session_id dans tous les appels /chat suivants.
    /// </summary>
    [HttpGet("session")]
    public async Task<IActionResult> NewSession()
        => Ok(await _service.CreateSessionAsync());

    /// <summary>
    /// Étape 2+ — Envoyer un message à l'agent IA.
    /// Appelez cet endpoint plusieurs fois avec le même session_id.
    /// Quand is_complete = true → result contient la spécialité + urgence
    /// + suggestedDoctorId pour pré-remplir la prise de RDV.
    /// Body : { sessionId, userMessage, language: "fr"|"ar"|"en" }
    /// </summary>
    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] AiChatMessageDto dto)
        => Ok(await _service.ChatAsync(dto));

    /// <summary>Réinitialiser la session (recommencer)</summary>
    [HttpDelete("session/{sessionId}")]
    public async Task<IActionResult> ResetSession(string sessionId)
    {
        await _service.ResetSessionAsync(sessionId);
        return NoContent();
    }
}
