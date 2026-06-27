namespace DataLayer.Entities;

/// <summary>
/// Represents a quiz attached to a lesson (one-to-one relationship with Lesson).
/// </summary>
public class Quiz : BaseGuidEntity
{
    public Guid LessonId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PassingScore { get; set; } = 70;         // Percentage (0-100)
    public int? TimeLimitMinutes { get; set; }
    public int MaxAttempts { get; set; } = 0;           // 0 = unlimited
    public bool ShuffleQuestions { get; set; } = false;
    public bool ShuffleAnswers { get; set; } = false;
    public bool ShowAnswersAfterSubmission { get; set; } = true;
    public bool IsPublished { get; set; } = true;

    // Navigation properties
    public Lesson Lesson { get; set; } = null!;
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();
}
