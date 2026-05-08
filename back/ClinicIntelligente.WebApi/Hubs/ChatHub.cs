namespace ClinicIntelligente.WebApi.Hubs;

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ClinicIntelligente.Core.Application.Interfaces.Services;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    public ChatHub(IChatService chatService) => _chatService = chatService;

    // ── Connection ────────────────────────────────────────────────
    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(userId));
        await base.OnDisconnectedAsync(exception);
    }

    // ── Send a message ────────────────────────────────────────────
    public async Task SendMessage(int receiverId, string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;

        var senderId = GetUserId();
        var dto      = await _chatService.SendAsync(senderId, receiverId, content);

        // Deliver to receiver and back to sender (all their tabs)
        await Clients.Group(UserGroup(receiverId)).SendAsync("ReceiveMessage", dto);
        await Clients.Group(UserGroup(senderId)).SendAsync("ReceiveMessage", dto);
    }

    // ── Delete a message (soft, per-user) ─────────────────────────
    public async Task DeleteMessage(int messageId)
    {
        var userId = GetUserId();
        await _chatService.DeleteMessageAsync(messageId, userId);

        // Notify caller so the UI removes it immediately
        await Clients.Group(UserGroup(userId)).SendAsync("MessageDeleted", messageId);
    }

    // ── Mark conversation as read ─────────────────────────────────
    public async Task MarkRead(int fromUserId)
    {
        var userId = GetUserId();
        await _chatService.MarkReadAsync(userId, fromUserId);
        await Clients.Group(UserGroup(userId)).SendAsync("ConversationRead", fromUserId);
    }

    // ── Helpers ───────────────────────────────────────────────────
    private int    GetUserId()          => int.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static string UserGroup(int id) => $"user-{id}";
}
