namespace ClinicIntelligente.Core.Application.Services;

using ClinicIntelligente.Core.Application.DTOs.Chat;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Application.Interfaces.Services;
using ClinicIntelligente.Core.Domain.Entities;

public class ChatService : IChatService
{
    private readonly IChatRepository _chat;
    private readonly IUserRepository _users;

    public ChatService(IChatRepository chat, IUserRepository users)
    {
        _chat  = chat;
        _users = users;
    }

    public async Task<ChatMessageDto> SendAsync(int senderId, int receiverId, string content)
    {
        var msg = new ChatMessage
        {
            SenderId   = senderId,
            ReceiverId = receiverId,
            Content    = content.Trim(),
            SentAt     = DateTime.UtcNow,
        };
        await _chat.AddAsync(msg);

        var sender   = await _users.GetByIdAsync(senderId);
        var receiver = await _users.GetByIdAsync(receiverId);

        return ToDto(msg, sender, receiver);
    }

    public async Task<IReadOnlyList<ChatMessageDto>> GetConversationAsync(int userId, int otherId)
    {
        var messages = await _chat.GetConversationAsync(userId, otherId);
        return messages.Select(m => ToDto(m, m.Sender, m.Receiver)).ToList();
    }

    public async Task<IReadOnlyList<ChatContactDto>> GetContactsAsync(int userId)
    {
        var contactIds = await _chat.GetContactIdsAsync(userId);
        var contacts   = new List<ChatContactDto>();

        foreach (var cid in contactIds)
        {
            var user      = await _users.GetByIdAsync(cid);
            if (user is null) continue;

            var conv      = await _chat.GetConversationAsync(userId, cid);
            var last      = conv.LastOrDefault();
            var unread    = await _chat.GetUnreadCountAsync(userId, cid);

            contacts.Add(new ChatContactDto(
                UserId:        cid,
                FullName:      $"{user.FirstName} {user.LastName}",
                Role:          user.Role.ToString(),
                UnreadCount:   unread,
                LastMessage:   last?.Content,
                LastMessageAt: last?.SentAt
            ));
        }

        return contacts.OrderByDescending(c => c.LastMessageAt).ToList();
    }

    public async Task MarkReadAsync(int userId, int fromUserId)
        => await _chat.MarkConversationReadAsync(userId, fromUserId);

    public async Task DeleteMessageAsync(int messageId, int userId)
        => await _chat.SoftDeleteAsync(messageId, userId);

    public async Task DeleteConversationAsync(int userId, int otherUserId)
        => await _chat.SoftDeleteConversationAsync(userId, otherUserId);

    public async Task<int> GetTotalUnreadAsync(int userId)
        => await _chat.GetTotalUnreadAsync(userId);

    private static ChatMessageDto ToDto(ChatMessage m, User? sender, User? receiver) => new(
        Id:           m.Id,
        SenderId:     m.SenderId,
        SenderName:   sender   is null ? "?" : $"{sender.FirstName} {sender.LastName}",
        ReceiverId:   m.ReceiverId,
        ReceiverName: receiver is null ? "?" : $"{receiver.FirstName} {receiver.LastName}",
        Content:      m.Content,
        SentAt:       m.SentAt,
        IsRead:       m.IsRead
    );
}
