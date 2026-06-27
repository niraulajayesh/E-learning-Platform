namespace DataLayer.Entities;

public class QuestionBankQuestion : BaseGuidEntity
{
    public int CategoryId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public string Subtopic { get; set; } = string.Empty;
    public string Difficulty { get; set; } = "Easy";
    public string Text { get; set; } = string.Empty;
    public string? QuestionImageUrl { get; set; }
    public string? ExplanationImageUrl { get; set; }
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public string? OptionE { get; set; }
    public string? OptionF { get; set; }
    public string CorrectOption { get; set; } = "A";
    public string Explanation { get; set; } = string.Empty;
    public string WrongAnswerExplanation { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public int EstimatedTimeSeconds { get; set; } = 60;
    public string Status { get; set; } = "Draft";
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
    public int DisplayOrder { get; set; }

    public Category Category { get; set; } = null!;
    public ICollection<PracticeTestQuestion> PracticeTestQuestions { get; set; } = new List<PracticeTestQuestion>();
}
