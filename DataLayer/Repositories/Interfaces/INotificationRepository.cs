using DataLayer.Entities;

namespace DataLayer.Repositories.Interfaces;

/// <summary>
/// Notification repository for inbox management and unread counting.
/// </summary>
public interface INotificationRepository : IRepository<Notification>
{
    Task<IEnumerable<Notification>> GetByUserAsync(Guid userId, int take = 20, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
    Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default);
}
