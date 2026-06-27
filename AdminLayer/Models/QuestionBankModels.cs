using DataLayer.Entities;
using Microsoft.AspNetCore.Http;

namespace AdminLayer.Models;

public class QuestionBankIndexViewModel
{
    public List<QuestionBankQuestion> Questions { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public string? Topic { get; set; }
    public string? Difficulty { get; set; }
    public string? Status { get; set; }
    public string? Tag { get; set; }
    public string Sort { get; set; } = "newest";
    public int TotalCount { get; set; }
    public int DraftCount { get; set; }
    public int PublishedCount { get; set; }
    public int ArchivedCount { get; set; }
}

public class QuestionBankImportViewModel
{
    public IFormFile? File { get; set; }
    public bool SaveValidRows { get; set; } = true;
    public List<string> Errors { get; set; } = new();
    public int ImportedCount { get; set; }
}

public class QuestionBankTestBuilderViewModel
{
    public List<Category> Categories { get; set; } = new();
    public int CategoryId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int EasyCount { get; set; } = 5;
    public int MediumCount { get; set; } = 5;
    public int HardCount { get; set; } = 0;
    public int TimeLimitMinutes { get; set; } = 15;
    public int PassingScorePercent { get; set; } = 70;
    public bool ShuffleQuestions { get; set; } = true;
    public bool ShuffleAnswers { get; set; } = true;
    public List<string> Errors { get; set; } = new();
}

public class MockExamBuilderViewModel
{
    public List<Category> Categories { get; set; } = new();
    public Dictionary<int, int> CategoryQuestionCounts { get; set; } = new();
    public string Title { get; set; } = "Full ASVAB Mock Exam";
    public string Description { get; set; } = "Generated from the central ASVAB question bank.";
    public int TimeLimitMinutes { get; set; } = 80;
    public int PassingScorePercent { get; set; } = 70;
    public bool ShuffleQuestions { get; set; } = true;
    public bool ShuffleAnswers { get; set; } = true;
    public List<string> Errors { get; set; } = new();
}
