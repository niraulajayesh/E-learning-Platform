namespace DataLayer.Entities;

/// <summary>
/// Represents a promotional discount coupon that can be applied at checkout.
/// </summary>
public class Coupon : BaseGuidEntity
{
    public string Code { get; set; } = string.Empty;           // Unique, case-insensitive
    public string? Description { get; set; }
    public int DiscountPercentage { get; set; } = 0;           // 1–100
    public decimal? MaxDiscountAmount { get; set; }            // Cap on discount value (nullable = no cap)
    public int MaxUses { get; set; } = 0;                      // 0 = unlimited
    public int UsedCount { get; set; } = 0;
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    // Optionally restrict to specific courses (null = all courses)
    public Guid? RestrictedToCourseId { get; set; }

    // Navigation properties
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
