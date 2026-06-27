namespace DataLayer.Entities;

public class FullExamAttempt : BaseGuidEntity
{
    public Guid? StudentId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public decimal ScorePercent { get; set; }
    public string CategoryBreakdownJson { get; set; } = string.Empty;
    public string AnswersJson { get; set; } = string.Empty;
    public string SummaryReport { get; set; } = string.Empty;

    public User? Student { get; set; }
}
