namespace SharedLayer.Enums;

/// <summary>
/// Status of a payment transaction.
/// </summary>
public enum PaymentStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4,
    Cancelled = 5,
    Disputed = 6
}
