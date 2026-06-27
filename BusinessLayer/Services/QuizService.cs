using BusinessLayer.Interfaces;
using DataLayer.Entities;
using DataLayer.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using SharedLayer.Enums;
using SharedLayer.Wrappers;
using System.Text.Json;

namespace BusinessLayer.Services;

public class QuizService : IQuizService
{
    private readonly IUnitOfWork _uow;

    public QuizService(IUnitOfWork uow) => _uow = uow;

    public async Task<Result<Quiz>> GetQuizByIdAsync(Guid id, CancellationToken ct = default)
    {
        var quiz = await _uow.Quizzes.Query()
            .Include(q => q.Lesson).ThenInclude(l => l.Section).ThenInclude(s => s.Course)
            .Include(q => q.Questions.OrderBy(q => q.Order)).ThenInclude(q => q.Answers.OrderBy(a => a.Order))
            .FirstOrDefaultAsync(q => q.Id == id, ct);
        if (quiz == null) return Result<Quiz>.Failure("Quiz not found.");
        return Result<Quiz>.Success(quiz);
    }

    public async Task<Result<Quiz>> GetQuizByLessonAsync(Guid lessonId, CancellationToken ct = default)
    {
        var quiz = await _uow.Quizzes.Query()
            .Include(q => q.Lesson).ThenInclude(l => l.Section).ThenInclude(s => s.Course)
            .Include(q => q.Questions.OrderBy(q => q.Order)).ThenInclude(q => q.Answers.OrderBy(a => a.Order))
            .FirstOrDefaultAsync(q => q.LessonId == lessonId, ct);
        if (quiz == null) return Result<Quiz>.Failure("Quiz not found.");
        return Result<Quiz>.Success(quiz);
    }

    public async Task<Result<Quiz>> CreateQuizAsync(Quiz quiz, CancellationToken ct = default)
    {
        if (await _uow.Quizzes.ExistsAsync(q => q.LessonId == quiz.LessonId, ct)) return Result<Quiz>.Failure("A quiz already exists for this lesson.");
        var validation = NormalizeQuiz(quiz);
        if (!validation.IsSuccess) return Result<Quiz>.Failure(validation.ErrorMessage!);
        await _uow.Quizzes.AddAsync(quiz, ct);
        await _uow.SaveChangesAsync(ct);
        return Result<Quiz>.Success(quiz);
    }

    public async Task<Result> UpdateQuizAsync(Quiz quiz, CancellationToken ct = default)
    {
        var existing = await _uow.Quizzes.Query()
            .Include(q => q.Questions).ThenInclude(q => q.Answers)
            .FirstOrDefaultAsync(q => q.Id == quiz.Id, ct);
        if (existing == null) return Result.Failure("Quiz not found.");

        existing.Title = quiz.Title;
        existing.Description = quiz.Description;
        existing.PassingScore = quiz.PassingScore;
        existing.TimeLimitMinutes = quiz.TimeLimitMinutes;
        existing.MaxAttempts = quiz.MaxAttempts;
        existing.ShuffleQuestions = quiz.ShuffleQuestions;
        existing.ShowAnswersAfterSubmission = quiz.ShowAnswersAfterSubmission;
        existing.Questions.Clear();
        foreach (var question in quiz.Questions) existing.Questions.Add(question);

        var validation = NormalizeQuiz(existing);
        if (!validation.IsSuccess) return Result.Failure(validation.ErrorMessage!);
        _uow.Quizzes.Update(existing);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteQuizAsync(Guid id, CancellationToken ct = default)
    {
        var quiz = await _uow.Quizzes.GetByIdAsync(id, ct);
        if (quiz == null) return Result.Failure("Quiz not found.");
        _uow.Quizzes.Remove(quiz);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<object>> GetQuizStatisticsAsync(Guid quizId, CancellationToken ct = default)
    {
        var attempts = await _uow.Quizzes.Query()
            .Where(q => q.Id == quizId)
            .SelectMany(q => q.Attempts)
            .ToListAsync(ct);
        var total = attempts.Count;
        var passRate = total == 0 ? 0 : Math.Round(attempts.Count(a => a.IsPassed) * 100.0 / total, 1);
        var averageScore = total == 0 ? 0 : Math.Round(attempts.Average(a => a.Score), 1);
        return Result<object>.Success(new { Attempts = total, PassRate = passRate, AverageScore = averageScore });
    }

    public async Task<Result<QuizAttempt>> SubmitQuizAsync(Guid quizId, Guid studentId, Dictionary<Guid, Guid> answers, int timeTakenSeconds = 0, CancellationToken ct = default)
    {
        var quiz = await _uow.Quizzes.GetWithQuestionsAsync(quizId, ct);
        if (quiz == null) return Result<QuizAttempt>.Failure("Quiz not found.");
        var attemptCount = await _uow.Quizzes.GetAttemptCountAsync(quizId, studentId, ct);
        if (quiz.MaxAttempts > 0 && attemptCount >= quiz.MaxAttempts) return Result<QuizAttempt>.Failure("Maximum number of attempts reached.");

        var totalPoints = 0;
        var earnedPoints = 0;
        foreach (var question in quiz.Questions.Where(q => q.Type == QuestionType.MultipleChoice))
        {
            totalPoints += question.Points;
            if (answers.TryGetValue(question.Id, out var selectedAnswerId))
            {
                var answer = question.Answers.FirstOrDefault(a => a.Id == selectedAnswerId);
                if (answer != null && answer.IsCorrect) earnedPoints += question.Points;
            }
        }

        var score = totalPoints > 0 ? Math.Round((double)earnedPoints / totalPoints * 100, 2) : 0;
        var attempt = new QuizAttempt
        {
            QuizId = quizId,
            StudentId = studentId,
            TotalPoints = totalPoints,
            EarnedPoints = earnedPoints,
            Score = score,
            IsPassed = score >= quiz.PassingScore,
            AttemptNumber = attemptCount + 1,
            AttemptedAt = DateTime.UtcNow,
            TimeTakenSeconds = Math.Max(0, timeTakenSeconds),
            AnswersSnapshot = JsonSerializer.Serialize(answers)
        };
        await _uow.Quizzes.AddAttemptAsync(attempt, ct);
        await _uow.SaveChangesAsync(ct);
        return Result<QuizAttempt>.Success(attempt);
    }

    public async Task<Result<IEnumerable<QuizAttempt>>> GetStudentQuizAttemptsAsync(Guid quizId, Guid studentId, CancellationToken ct = default)
    {
        var attempts = await _uow.Quizzes.GetAttemptsAsync(quizId, studentId, ct);
        return Result<IEnumerable<QuizAttempt>>.Success(attempts);
    }

    public async Task<Result<IEnumerable<QuizAttempt>>> GetStudentQuizAttemptsAsync(Guid studentId, CancellationToken ct = default)
    {
        var attempts = await _uow.Quizzes.Query()
            .SelectMany(q => q.Attempts)
            .Where(a => a.StudentId == studentId)
            .Include(a => a.Quiz).ThenInclude(q => q.Lesson).ThenInclude(l => l.Section).ThenInclude(s => s.Course)
            .OrderByDescending(a => a.AttemptedAt)
            .ToListAsync(ct);
        return Result<IEnumerable<QuizAttempt>>.Success(attempts);
    }

    private static Result NormalizeQuiz(Quiz quiz)
    {
        if (string.IsNullOrWhiteSpace(quiz.Title)) return Result.Failure("Quiz title is required.");
        var questions = quiz.Questions.Where(q => !string.IsNullOrWhiteSpace(q.Text)).OrderBy(q => q.Order).ToList();
        if (!questions.Any()) return Result.Failure("Add at least one question.");
        for (var i = 0; i < questions.Count; i++)
        {
            var question = questions[i];
            question.Type = QuestionType.MultipleChoice;
            question.Order = i + 1;
            question.Points = Math.Max(1, question.Points);
            var answers = question.Answers.Where(a => !string.IsNullOrWhiteSpace(a.Text)).OrderBy(a => a.Order).ToList();
            if (answers.Count < 2) return Result.Failure($"Question {i + 1} needs at least two answers.");
            if (answers.Count(a => a.IsCorrect) != 1) return Result.Failure($"Question {i + 1} must have exactly one correct answer.");
            question.Answers = answers.Select((answer, index) => { answer.Order = index + 1; return answer; }).ToList();
        }
        quiz.Questions = questions;
        quiz.PassingScore = Math.Clamp(quiz.PassingScore, 0, 100);
        quiz.MaxAttempts = Math.Max(0, quiz.MaxAttempts);
        return Result.Success();
    }
}
