namespace ClinicIntelligente.Core.Application.Interfaces.Repositories;

using ClinicIntelligente.Core.Domain.Entities;

public interface INotificationRepository : IGenericRepository<Notification>
{
    Task<IReadOnlyList<Notification>> GetByRecipientIdAsync(int recipientId);
    Task<int> GetUnreadCountAsync(int recipientId);
    Task MarkAllAsReadAsync(int recipientId);
    Task DeleteAsync(int notificationId, int recipientId);
}
