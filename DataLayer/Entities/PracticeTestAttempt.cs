namespace DataLayer.Entities;

public class PracticeTestAttempt : BaseGuidEntity
{
    public Guid PracticeTestId { get; set; }
    public Guid? StudentId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public decimal ScorePercent { get; set; }
    public string AnswersJson { get; set; } = string.Empty;

    public PracticeTest PracticeTest { get; set; } = null!;
    public User? Student { get; set; }
}
