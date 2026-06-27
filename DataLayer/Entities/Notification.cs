namespace DataLayer.Entities;

/// <summary>
/// Represents an in-app notification sent to a user (enrollment confirmation, new lesson, etc.).
/// </summary>
public class Notification : BaseGuidEntity
{
    public Guid UserId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }              // Deep link (e.g., /courses/{id})
    public string? IconClass { get; set; }              // CSS icon class (e.g., "fa-graduation-cap")
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
}
