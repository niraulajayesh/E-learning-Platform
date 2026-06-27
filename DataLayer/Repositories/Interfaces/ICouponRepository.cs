using DataLayer.Entities;

namespace DataLayer.Repositories.Interfaces;

/// <summary>
/// Coupon repository for validation, usage tracking, and lookup.
/// </summary>
public interface ICouponRepository : IRepository<Coupon>
{
    Task<Coupon?> GetByCodeAsync(string code, CancellationToken ct = default);
    Task<bool> IsValidAsync(string code, CancellationToken ct = default);
    Task IncrementUsageAsync(Guid couponId, CancellationToken ct = default);
}
