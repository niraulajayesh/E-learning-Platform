namespace DataLayer.Entities;

public class PracticeTestQuestion : BaseGuidEntity
{
    public Guid PracticeTestId { get; set; }
    public Guid? QuestionBankQuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string OptionA { get; set; } = string.Empty;
    public string OptionB { get; set; } = string.Empty;
    public string OptionC { get; set; } = string.Empty;
    public string OptionD { get; set; } = string.Empty;
    public string? OptionE { get; set; }
    public string? OptionF { get; set; }
    public string CorrectOption { get; set; } = "A";
    public string Explanation { get; set; } = string.Empty;
    public int Order { get; set; }

    public PracticeTest PracticeTest { get; set; } = null!;
    public QuestionBankQuestion? QuestionBankQuestion { get; set; }
}
