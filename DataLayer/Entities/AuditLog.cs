namespace DataLayer.Entities;

/// <summary>
/// Immutable record of significant actions performed in the system (admin changes, publishing, etc.).
/// Uses a long identity PK for high-volume insert performance.
/// </summary>
public class AuditLog
{
    public long Id { get; set; }
    public Guid? UserId { get; set; }                   // Null for system-generated actions

    public string Action { get; set; } = string.Empty;  // e.g., "Course.Published"
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string? OldValues { get; set; }              // JSON snapshot before change
    public string? NewValues { get; set; }              // JSON snapshot after change
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User? User { get; set; }
}
