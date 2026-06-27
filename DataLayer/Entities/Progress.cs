namespace DataLayer.Entities;

/// <summary>
/// Tracks per-lesson progress for a student's enrollment.
/// One progress record per (Enrollment + Lesson) pair.
/// </summary>
public class Progress : BaseGuidEntity
{
    public Guid EnrollmentId { get; set; }
    public Guid LessonId { get; set; }

    public bool IsCompleted { get; set; } = false;
    public int WatchedSeconds { get; set; } = 0;      // For video lessons
    public DateTime? LastWatchedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public Enrollment Enrollment { get; set; } = null!;
    public Lesson Lesson { get; set; } = null!;
}
