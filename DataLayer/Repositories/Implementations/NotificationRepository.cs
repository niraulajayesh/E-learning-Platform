using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories.Implementations;

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(AppDbContext context) : base(context) { }

    public async Task<IEnumerable<Notification>> GetByUserAsync(Guid userId, int take = 20, CancellationToken ct = default)
        => await _dbSet
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

    public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
        => await _dbSet.CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await _dbSet
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now), ct);
    }
}
