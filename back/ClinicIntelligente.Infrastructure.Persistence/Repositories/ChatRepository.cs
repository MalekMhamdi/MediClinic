namespace ClinicIntelligente.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using ClinicIntelligente.Core.Application.Interfaces.Repositories;
using ClinicIntelligente.Core.Domain.Entities;
using ClinicIntelligente.Infrastructure.Persistence.Contexts;

public class ChatRepository : GenericRepository<ChatMessage>, IChatRepository
{
    public ChatRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IReadOnlyList<ChatMessage>> GetConversationAsync(int userId1, int userId2)
        => await _context.ChatMessages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m =>
                (m.SenderId == userId1 && m.ReceiverId == userId2 && !m.IsDeletedBySender) ||
                (m.SenderId == userId2 && m.ReceiverId == userId1 && !m.IsDeletedByReceiver))
            .OrderBy(m => m.SentAt)
            .ToListAsync();

    public async Task<IReadOnlyList<int>> GetContactIdsAsync(int userId)
        => await _context.ChatMessages
            .Where(m => (m.SenderId == userId && !m.IsDeletedBySender) ||
                        (m.ReceiverId == userId && !m.IsDeletedByReceiver))
            .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Distinct()
            .ToListAsync();

    public async Task<int> GetUnreadCountAsync(int userId, int fromUserId)
        => await _context.ChatMessages
            .CountAsync(m => m.ReceiverId == userId && m.SenderId == fromUserId && !m.IsRead);

    public async Task<int> GetTotalUnreadAsync(int userId)
        => await _context.ChatMessages
            .CountAsync(m => m.ReceiverId == userId && !m.IsRead && !m.IsDeletedByReceiver);

    public async Task MarkConversationReadAsync(int userId, int fromUserId)
    {
        var unread = await _context.ChatMessages
            .Where(m => m.ReceiverId == userId && m.SenderId == fromUserId && !m.IsRead)
            .ToListAsync();

        foreach (var msg in unread)
        {
            msg.IsRead = true;
            msg.LastModifiedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
    }

    public async Task SoftDeleteConversationAsync(int userId, int otherUserId)
    {
        var messages = await _context.ChatMessages
            .Where(m =>
                (m.SenderId == userId   && m.ReceiverId == otherUserId) ||
                (m.SenderId == otherUserId && m.ReceiverId == userId))
            .ToListAsync();

        foreach (var m in messages)
        {
            if (m.SenderId   == userId) m.IsDeletedBySender   = true;
            if (m.ReceiverId == userId) m.IsDeletedByReceiver = true;
            m.LastModifiedAt = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();
    }

    public async Task SoftDeleteAsync(int messageId, int userId)
    {
        var msg = await _context.ChatMessages.FindAsync(messageId);
        if (msg is null) return;

        if (msg.SenderId == userId)   msg.IsDeletedBySender   = true;
        if (msg.ReceiverId == userId) msg.IsDeletedByReceiver = true;

        msg.LastModifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}
