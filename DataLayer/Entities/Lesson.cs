using SharedLayer.Enums;

namespace DataLayer.Entities;

/// <summary>
/// Represents a single lesson (video, article, quiz, assignment) within a course section.
/// </summary>
public class Lesson : BaseGuidEntity
{
    public Guid SectionId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public LessonType Type { get; set; } = LessonType.Video;
    public string? ContentUrl { get; set; }      // Video URL or uploaded video path
    public string? ArticleContent { get; set; }  // Rich HTML content for article-type lessons
    public string? ResourcesUrl { get; set; }    // Downloadable resources, including uploaded PDF path

    public int DurationMinutes { get; set; } = 0;
    public int Order { get; set; } = 0;
    public bool IsFreePreview { get; set; } = false;
    public bool IsPublished { get; set; } = true;

    // Navigation properties
    public Section Section { get; set; } = null!;
    public Quiz? Quiz { get; set; }
    public ICollection<Progress> ProgressRecords { get; set; } = new List<Progress>();
}
