using System.Reflection;
using System.Text.Json;
using BusinessLayer.Interfaces;
using DataLayer.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UserLayer.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IQuizService _quizService;
    private readonly ICertificateService _certificateService;
    private readonly AppDbContext _db;

    public DashboardController(IDashboardService dashboardService, IEnrollmentService enrollmentService, IQuizService quizService, ICertificateService certificateService, AppDbContext db)
    {
        _dashboardService = dashboardService;
        _enrollmentService = enrollmentService;
        _quizService = quizService;
        _certificateService = certificateService;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var studentId = Guid.Parse(User.FindFirst("UserId")!.Value);
        ViewData["Title"] = "My Dashboard";

        var metricsResult = await _dashboardService.GetStudentDashboardMetricsAsync(studentId);
        var enrollmentsResult = await _enrollmentService.GetStudentEnrollmentsAsync(studentId);
        var quizAttemptsResult = await _quizService.GetStudentQuizAttemptsAsync(studentId);
        var certificatesResult = await _certificateService.GetStudentCertificatesAsync(studentId);

        var metrics = metricsResult.Data;
        ViewBag.ActiveCourses = GetMetric(metrics, "ActiveCourses") ?? 0;
        ViewBag.CompletedCourses = GetMetric(metrics, "CompletedCourses") ?? 0;
        ViewBag.TotalCertificates = GetMetric(metrics, "TotalCertificates") ?? 0;
        ViewBag.RecentEnrollments = (enrollmentsResult.Data ?? Enumerable.Empty<DataLayer.Entities.Enrollment>())
            .OrderByDescending(e => e.EnrolledAt).Take(4).ToList();
        ViewBag.RecentQuizAttempts = (quizAttemptsResult.Data ?? Enumerable.Empty<DataLayer.Entities.QuizAttempt>())
            .OrderByDescending(a => a.AttemptedAt).Take(5).ToList();
        ViewBag.RecentCertificates = (certificatesResult.Data ?? Enumerable.Empty<DataLayer.Entities.Certificate>())
            .OrderByDescending(c => c.IssuedAt).Take(4).ToList();
        ViewBag.AsvabProgress = await BuildAsvabProgressAsync(studentId);
        ViewBag.WeakAreas = await BuildWeakAreasAsync(studentId);

        return View();
    }

    private static object? GetMetric(object? source, string name)
        => source?.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(source);
    private async Task<List<AsvabProgressMetric>> BuildAsvabProgressAsync(Guid studentId)
    {
        var categories = await _db.Categories
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
        var enrollments = await _db.Enrollments
            .Include(e => e.Course)
            .Where(e => e.StudentId == studentId)
            .ToListAsync();
        var practiceAttempts = await _db.PracticeTestAttempts
            .Include(a => a.PracticeTest)
            .ThenInclude(t => t.Category)
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.CompletedAt)
            .ToListAsync();

        return categories.Select(category =>
        {
            var categoryEnrollments = enrollments.Where(e => e.Course?.CategoryId == category.Id).ToList();
            var completion = categoryEnrollments.Any()
                ? (decimal)categoryEnrollments.Average(e => e.CompletionPercentage)
                : 0m;
            var categoryAttempts = practiceAttempts
                .Where(a => a.PracticeTest?.CategoryId == category.Id)
                .Take(5)
                .ToList();
            var recentScores = categoryAttempts.Select(a => a.ScorePercent).ToList();
            var averageScore = recentScores.Any() ? recentScores.Average() : 0m;
            var progress = Math.Round((completion * 0.6m) + (averageScore * 0.4m), 1);

            return new AsvabProgressMetric(
                category.Name.Split(' ')[0],
                category.Name,
                progress,
                Math.Round(completion, 1),
                Math.Round(averageScore, 1),
                recentScores.Select(s => Math.Round(s, 1)).ToList());
        }).ToList();
    }

    public sealed record AsvabProgressMetric(
        string Code,
        string Category,
        decimal ProgressPercent,
        decimal CompletionPercent,
        decimal AverageRecentScore,
        List<decimal> RecentScores);

    private async Task<List<WeakAreaRecommendation>> BuildWeakAreasAsync(Guid studentId)
    {
        var quizAttempts = await _db.QuizAttempts
            .Include(a => a.Quiz)
            .ThenInclude(q => q.Lesson)
            .ThenInclude(l => l.Section)
            .ThenInclude(s => s.Course)
            .ThenInclude(c => c.Category)
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.AttemptedAt)
            .ToListAsync();
        var practiceAttempts = await _db.PracticeTestAttempts
            .Include(a => a.PracticeTest)
            .ThenInclude(t => t.Category)
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.CompletedAt)
            .ToListAsync();
        var fullExamAttempts = await _db.FullExamAttempts
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.CompletedAt)
            .Take(3)
            .ToListAsync();

        var scoresByCategory = new Dictionary<string, List<decimal>>(StringComparer.OrdinalIgnoreCase);
        void AddScore(string? category, decimal score)
        {
            if (string.IsNullOrWhiteSpace(category)) return;
            if (!scoresByCategory.TryGetValue(category, out var scores))
            {
                scores = new List<decimal>();
                scoresByCategory[category] = scores;
            }
            scores.Add(Math.Clamp(score, 0m, 100m));
        }

        foreach (var attempt in quizAttempts)
        {
            AddScore(attempt.Quiz?.Lesson?.Section?.Course?.Category?.Name, (decimal)attempt.Score);
        }

        foreach (var attempt in practiceAttempts)
        {
            AddScore(attempt.PracticeTest?.Category?.Name, attempt.ScorePercent);
        }

        foreach (var attempt in fullExamAttempts)
        {
            if (string.IsNullOrWhiteSpace(attempt.CategoryBreakdownJson)) continue;
            try
            {
                var breakdown = JsonSerializer.Deserialize<List<ExamBreakdownMetric>>(attempt.CategoryBreakdownJson) ?? new();
                foreach (var item in breakdown)
                {
                    AddScore(item.Category, item.ScorePercent);
                }
            }
            catch (JsonException)
            {
                // Ignore malformed historic attempt data and keep recommendations from other signals.
            }
        }

        return scoresByCategory
            .Select(kvp => new WeakAreaRecommendation(
                kvp.Key.Split(' ')[0],
                kvp.Key,
                Math.Round(kvp.Value.Average(), 1),
                kvp.Value.Count,
                BuildFocusReason(kvp.Value.Average(), kvp.Value.Count)))
            .OrderBy(area => area.AverageScore)
            .ThenByDescending(area => area.SignalCount)
            .Take(3)
            .ToList();
    }

    private static string BuildFocusReason(decimal averageScore, int signalCount)
    {
        if (averageScore < 60m) return $"Priority review from {signalCount} recent result{(signalCount == 1 ? "" : "s")}.";
        if (averageScore < 75m) return $"Developing area from {signalCount} recent result{(signalCount == 1 ? "" : "s")}.";
        return $"Smallest current margin from {signalCount} recent result{(signalCount == 1 ? "" : "s")}.";
    }

    public sealed record WeakAreaRecommendation(
        string Code,
        string Category,
        decimal AverageScore,
        int SignalCount,
        string Reason);

    private sealed record ExamBreakdownMetric(
        string Category,
        int TotalQuestions,
        int CorrectAnswers,
        decimal ScorePercent);
}



