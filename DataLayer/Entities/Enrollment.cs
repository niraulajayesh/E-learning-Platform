using SharedLayer.Enums;

namespace DataLayer.Entities;

/// <summary>
/// Represents a student's enrollment in a course.
/// One student can enroll in many courses; one course can have many enrolled students.
/// </summary>
public class Enrollment : BaseGuidEntity
{
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public double CompletionPercentage { get; set; } = 0.0;
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }

    // Navigation properties
    public User Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
    public Certificate? Certificate { get; set; }
    public ICollection<Progress> ProgressRecords { get; set; } = new List<Progress>();
}
