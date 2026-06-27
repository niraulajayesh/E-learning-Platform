using SharedLayer.Enums;

namespace DataLayer.Entities;

/// <summary>
/// Represents a payment transaction made by a student to enroll in a course.
/// </summary>
public class Payment : BaseGuidEntity
{
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }
    public Guid? CouponId { get; set; }

    public decimal OriginalAmount { get; set; }         // Price before discount
    public decimal DiscountAmount { get; set; } = 0m;   // Amount saved by coupon
    public decimal Amount { get; set; }                 // Final charged amount

    public string Currency { get; set; } = "USD";
    public string? GatewayName { get; set; }            // e.g., "Stripe", "PayPal"
    public string? GatewayReference { get; set; }       // External transaction ID
    public string? GatewayResponse { get; set; }        // Raw JSON response from gateway
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public User Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public Coupon? Coupon { get; set; }
}
