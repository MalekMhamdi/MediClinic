

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClinicIntelligente.Core.Application.DTOs.Notification;
using ClinicIntelligente.Core.Application.Interfaces.Services;

namespace ClinicIntelligente.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _service;
    public NotificationController(INotificationService service) => _service = service;

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    /// <summary>
    /// Get all my notifications, ordered by newest first.
    /// Works for Patient, Doctor, Admin and LabTechnicien.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), 200)]
    public async Task<IActionResult> GetMine()
        => Ok(await _service.GetMyNotificationsAsync(GetUserId()));

    /// <summary>
    /// Get count of unread notifications.
    /// Used by Angular to show the badge number on the bell icon.
    /// </summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _service.GetUnreadCountAsync(GetUserId());
        return Ok(new { unreadCount = count });
    }

    /// <summary>
    /// Mark a single notification as read.
    /// </summary>
    [HttpPatch("{id:int}/read")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await _service.MarkAsReadAsync(id, GetUserId());
        return NoContent();
    }

    /// <summary>
    /// Mark ALL my notifications as read.
    /// </summary>
    [HttpPatch("read-all")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _service.MarkAllAsReadAsync(GetUserId());
        return NoContent();
    }

    /// <summary>
    /// Delete a notification. Only the owner can delete it.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id, GetUserId());
        return NoContent();
    }
}