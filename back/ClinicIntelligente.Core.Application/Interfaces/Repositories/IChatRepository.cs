namespace ClinicIntelligente.Core.Application.Interfaces.Repositories;

using ClinicIntelligente.Core.Domain.Entities;

public interface IChatRepository : IGenericRepository<ChatMessage>
{
    Task<IReadOnlyList<ChatMessage>> GetConversationAsync(int userId1, int userId2);
    Task<IReadOnlyList<int>>         GetContactIdsAsync(int userId);
    Task<int>                        GetUnreadCountAsync(int userId, int fromUserId);
    Task                             MarkConversationReadAsync(int userId, int fromUserId);
    Task                             SoftDeleteAsync(int messageId, int userId);
    Task                             SoftDeleteConversationAsync(int userId, int otherUserId);
    Task<int>                        GetTotalUnreadAsync(int userId);
}
