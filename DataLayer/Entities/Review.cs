namespace DataLayer.Entities;

/// <summary>
/// Represents a student's star rating and written review for a course they are enrolled in.
/// A student can leave only one review per course.
/// </summary>
public class Review : BaseGuidEntity
{
    public Guid CourseId { get; set; }
    public Guid StudentId { get; set; }

    public int Rating { get; set; }                     // 1 to 5
    public string? Comment { get; set; }
    public bool IsVisible { get; set; } = true;
    public string? InstructorReply { get; set; }
    public DateTime? InstructorRepliedAt { get; set; }

    // Navigation properties
    public Course Course { get; set; } = null!;
    public User Student { get; set; } = null!;
}
