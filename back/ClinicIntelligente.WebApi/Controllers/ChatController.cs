namespace ClinicIntelligente.WebApi.Controllers;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ClinicIntelligente.Core.Application.DTOs.Chat;
using ClinicIntelligente.Core.Application.Interfaces.Services;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chat;
    public ChatController(IChatService chat) => _chat = chat;

    private int UserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");

    /// <summary>Get conversation history with another user</summary>
    [HttpGet("history/{otherUserId:int}")]
    public async Task<IActionResult> GetHistory(int otherUserId)
        => Ok(await _chat.GetConversationAsync(UserId(), otherUserId));

    /// <summary>List all contacts (users I've chatted with)</summary>
    [HttpGet("contacts")]
    public async Task<IActionResult> GetContacts()
        => Ok(await _chat.GetContactsAsync(UserId()));

    /// <summary>Total unread message count (for badge)</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
        => Ok(new { unreadCount = await _chat.GetTotalUnreadAsync(UserId()) });

    /// <summary>Mark all messages from a user as read</summary>
    [HttpPatch("read/{fromUserId:int}")]
    public async Task<IActionResult> MarkRead(int fromUserId)
    {
        await _chat.MarkReadAsync(UserId(), fromUserId);
        return NoContent();
    }

    /// <summary>Delete entire conversation with another user (soft delete)</summary>
    [HttpDelete("conversation/{otherUserId:int}")]
    public async Task<IActionResult> DeleteConversation(int otherUserId)
    {
        await _chat.DeleteConversationAsync(UserId(), otherUserId);
        return NoContent();
    }
}
