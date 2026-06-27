namespace DataLayer.Entities;

public class PracticeTest : BaseGuidEntity
{
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsTimed { get; set; }
    public int? TimeLimitMinutes { get; set; }
    public int PassingScorePercent { get; set; } = 70;
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleAnswers { get; set; }
    public bool IsMockExam { get; set; }
    public bool IsPublished { get; set; } = true;
    public int DisplayOrder { get; set; }

    public Category Category { get; set; } = null!;
    public ICollection<PracticeTestQuestion> Questions { get; set; } = new List<PracticeTestQuestion>();
    public ICollection<PracticeTestAttempt> Attempts { get; set; } = new List<PracticeTestAttempt>();
}
