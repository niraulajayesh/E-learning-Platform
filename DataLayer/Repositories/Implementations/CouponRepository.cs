using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories.Implementations;

public class CouponRepository : Repository<Coupon>, ICouponRepository
{
    public CouponRepository(AppDbContext context) : base(context) { }

    public async Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(c =>
            c.Code.ToLower() == code.ToLower(), ct);

    public async Task<bool> IsValidAsync(string code, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet.AnyAsync(c =>
            c.Code.ToLower() == code.ToLower() &&
            c.IsActive &&
            (c.StartsAt == null || c.StartsAt <= now) &&
            (c.ExpiresAt == null || c.ExpiresAt > now) &&
            (c.MaxUses == 0 || c.UsedCount < c.MaxUses), ct);
    }

    public async Task IncrementUsageAsync(Guid couponId, CancellationToken ct = default)
    {
        // ExecuteUpdateAsync for atomic, concurrency-safe increment
        await _dbSet
            .Where(c => c.Id == couponId)
            .ExecuteUpdateAsync(s =>
                s.SetProperty(c => c.UsedCount, c => c.UsedCount + 1), ct);
    }
}
