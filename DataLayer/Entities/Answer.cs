namespace DataLayer.Entities;

/// <summary>
/// Represents one possible answer option for a quiz question.
/// </summary>
public class Answer : BaseGuidEntity
{
    public Guid QuestionId { get; set; }

    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; } = false;
    public int Order { get; set; } = 0;

    // Navigation properties
    public Question Question { get; set; } = null!;
}
