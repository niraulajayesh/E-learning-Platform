using System.Security.Claims;
using System.Text.Json;
using DataLayer.Context;
using DataLayer.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UserLayer.Controllers;

public class ExamSimulatorController : Controller
{
    private readonly AppDbContext _db;
    private static readonly Dictionary<string, int> SectionMinutes = new()
    {
        ["WK"] = 8, ["PC"] = 8, ["AR"] = 12, ["MK"] = 10, ["GS"] = 8, ["EI"] = 8, ["MC"] = 8, ["AS"] = 8, ["AO"] = 8
    };

    public ExamSimulatorController(AppDbContext db) => _db = db;

    public IActionResult Index()
    {
        ViewData["Title"] = "Full ASVAB Mock Exam";
        ViewBag.SectionMinutes = SectionMinutes;
        ViewBag.TotalMinutes = SectionMinutes.Values.Sum();
        return View();
    }

    public async Task<IActionResult> Start()
    {
        var questions = await BuildExamQuestions();
        ViewData["Title"] = "Full ASVAB Mock Exam";
        ViewBag.SectionMinutes = SectionMinutes;
        ViewBag.TotalMinutes = SectionMinutes.Values.Sum();
        return View(questions);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Dictionary<Guid, string> answers)
    {
        var ids = answers.Keys.ToList();
        var questions = await _db.QuestionBankQuestions.Include(q => q.Category).Where(q => ids.Contains(q.Id)).ToListAsync();
        if (!questions.Any()) return RedirectToAction(nameof(Index));

        var total = questions.Count;
        var correct = questions.Count(q => answers.TryGetValue(q.Id, out var selected) && string.Equals(selected, q.CorrectOption, StringComparison.OrdinalIgnoreCase));
        var breakdown = questions.GroupBy(q => q.Category.Name).Select(g =>
        {
            var categoryTotal = g.Count();
            var categoryCorrect = g.Count(q => answers.TryGetValue(q.Id, out var selected) && string.Equals(selected, q.CorrectOption, StringComparison.OrdinalIgnoreCase));
            return new ExamCategoryScore(g.Key, categoryTotal, categoryCorrect, Math.Round((decimal)categoryCorrect * 100m / categoryTotal, 2));
        }).OrderBy(s => s.Category).ToList();
        var score = Math.Round((decimal)correct * 100m / total, 2);
        var summary = BuildSummary(score, breakdown);

        var attempt = new FullExamAttempt
        {
            StudentId = TryGetCurrentUserId(out var userId) ? userId : null,
            TotalQuestions = total,
            CorrectAnswers = correct,
            ScorePercent = score,
            CategoryBreakdownJson = JsonSerializer.Serialize(breakdown),
            AnswersJson = JsonSerializer.Serialize(answers.ToDictionary(a => a.Key.ToString(), a => a.Value)),
            SummaryReport = summary,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow
        };
        _db.FullExamAttempts.Add(attempt);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Results), new { id = attempt.Id });
    }

    public async Task<IActionResult> Results(Guid id)
    {
        var attempt = await _db.FullExamAttempts.FirstOrDefaultAsync(a => a.Id == id);
        if (attempt == null) return NotFound();
        ViewData["Title"] = "ASVAB Exam Results";
        return View(attempt);
    }

    private async Task<List<QuestionBankQuestion>> BuildExamQuestions()
    {
        var categories = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
        var exam = new List<QuestionBankQuestion>();
        foreach (var category in categories)
        {
            var questions = await _db.QuestionBankQuestions.Include(q => q.Category)
                .Where(q => q.CategoryId == category.Id && q.Status == "Published")
                .OrderBy(q => q.Difficulty == "Easy" ? 0 : q.Difficulty == "Medium" ? 1 : 2)
                .ThenBy(q => q.DisplayOrder)
                .Take(9)
                .ToListAsync();
            exam.AddRange(questions);
        }
        return exam;
    }

    private static string BuildSummary(decimal score, List<ExamCategoryScore> breakdown)
    {
        var strongest = breakdown.OrderByDescending(b => b.ScorePercent).FirstOrDefault();
        var weakest = breakdown.OrderBy(b => b.ScorePercent).FirstOrDefault();
        var readiness = score >= 80 ? "Strong readiness" : score >= 65 ? "Developing readiness" : "Needs focused review";
        return $"{readiness}. Overall score: {score}%. Strongest section: {strongest?.Category ?? "n/a"}. Priority review section: {weakest?.Category ?? "n/a"}. Review missed categories with study guides, flashcards, and practice tests before retaking the mock exam.";
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var value = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out userId);
    }

    public sealed record ExamCategoryScore(string Category, int TotalQuestions, int CorrectAnswers, decimal ScorePercent);
}

