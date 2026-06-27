namespace DataLayer.Entities;

/// <summary>
/// Represents a logical grouping of lessons within a course (e.g., "Module 1: Introduction").
/// </summary>
public class Section : BaseGuidEntity
{
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; } = 0;

    // Navigation properties
    public Course Course { get; set; } = null!;
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
