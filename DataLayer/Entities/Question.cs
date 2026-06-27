using SharedLayer.Enums;

namespace DataLayer.Entities;

/// <summary>
/// Represents a single question within a quiz.
/// </summary>
public class Question : BaseGuidEntity
{
    public Guid QuizId { get; set; }
    public Guid? QuestionBankQuestionId { get; set; }
    public int? CategoryId { get; set; }

    public string Text { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Explanation { get; set; }    // Shown after answering
    public string Difficulty { get; set; } = "Easy";
    public QuestionType Type { get; set; } = QuestionType.MultipleChoice;
    public int Points { get; set; } = 1;
    public int Order { get; set; } = 0;

    // Navigation properties
    public Quiz Quiz { get; set; } = null!;
    public Category? Category { get; set; }
    public QuestionBankQuestion? QuestionBankQuestion { get; set; }
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
}
