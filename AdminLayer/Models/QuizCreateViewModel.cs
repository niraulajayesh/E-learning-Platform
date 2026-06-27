using DataLayer.Entities;

namespace AdminLayer.Models;

public class QuizCreateViewModel
{
    public Guid Id { get; set; }
    public Guid LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PassingScore { get; set; } = 70;
    public int? TimeLimitMinutes { get; set; }
    public int MaxAttempts { get; set; } = 0;
    public bool ShuffleQuestions { get; set; }
    public bool ShuffleAnswers { get; set; }
    public bool ShowAnswersAfterSubmission { get; set; } = true;
    public bool IsPublished { get; set; } = true;
    public List<QuizQuestionInputModel> Questions { get; set; } = new();
}

public class QuizQuestionInputModel
{
    public Guid Id { get; set; }
    public Guid? QuestionBankQuestionId { get; set; }
    public int? CategoryId { get; set; }
    public string Difficulty { get; set; } = "Easy";
    public string Text { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Explanation { get; set; }
    public int Points { get; set; } = 1;
    public int Order { get; set; }
    public int CorrectAnswerIndex { get; set; }
    public List<string> Answers { get; set; } = new() { "", "", "", "", "", "" };
}

public class QuizIndexViewModel
{
    public List<Quiz> Quizzes { get; set; } = new();
    public List<Lesson> Lessons { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public string? Search { get; set; }
    public bool? IsPublished { get; set; }
}
