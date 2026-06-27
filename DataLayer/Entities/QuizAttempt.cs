namespace DataLayer.Entities;

/// <summary>
/// Records a single student attempt at a quiz, capturing score and pass/fail result.
/// </summary>
public class QuizAttempt : BaseGuidEntity
{
    public Guid QuizId { get; set; }
    public Guid StudentId { get; set; }

    public double Score { get; set; } = 0.0;           // Achieved score as percentage
    public int TotalPoints { get; set; } = 0;
    public int EarnedPoints { get; set; } = 0;
    public bool IsPassed { get; set; } = false;
    public int AttemptNumber { get; set; } = 1;
    public int TimeTakenSeconds { get; set; } = 0;
    public string? AnswersSnapshot { get; set; }        // JSON: student's selected answer IDs
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Quiz Quiz { get; set; } = null!;
    public User Student { get; set; } = null!;
}
