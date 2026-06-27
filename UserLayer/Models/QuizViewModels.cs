using DataLayer.Entities;

namespace UserLayer.Models;

public class QuizTakeViewModel
{
    public Quiz Quiz { get; set; } = null!;
    public IReadOnlyList<QuizAttempt> Attempts { get; set; } = Array.Empty<QuizAttempt>();
    public bool CanAttempt { get; set; } = true;
    public string? LockReason { get; set; }
}

public class QuizResultViewModel
{
    public Quiz Quiz { get; set; } = null!;
    public QuizAttempt Attempt { get; set; } = null!;
    public IReadOnlyList<QuizAttempt> Attempts { get; set; } = Array.Empty<QuizAttempt>();
    public IReadOnlyDictionary<Guid, Guid> SelectedAnswers { get; set; } = new Dictionary<Guid, Guid>();
}
