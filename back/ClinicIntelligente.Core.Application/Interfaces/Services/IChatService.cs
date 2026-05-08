namespace ClinicIntelligente.Core.Application.Interfaces.Services;

using ClinicIntelligente.Core.Application.DTOs.Chat;

public interface IChatService
{
    Task<ChatMessageDto>           SendAsync(int senderId, int receiverId, string content);
    Task<IReadOnlyList<ChatMessageDto>> GetConversationAsync(int userId, int otherUserId);
    Task<IReadOnlyList<ChatContactDto>> GetContactsAsync(int userId);
    Task                           MarkReadAsync(int userId, int fromUserId);
    Task                           DeleteMessageAsync(int messageId, int userId);
    Task                           DeleteConversationAsync(int userId, int otherUserId);
    Task<int>                      GetTotalUnreadAsync(int userId);
}
