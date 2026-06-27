using System.Security.Claims;
using System.Text.Json;
using DataLayer.Context;
using DataLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UserLayer.Controllers;

[Authorize]
public class StudyPlannerController : Controller
{
    private readonly AppDbContext _db;

    public StudyPlannerController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var studentId = GetStudentId();
        var planner = await _db.StudyPlanners.FirstOrDefaultAsync(p => p.StudentId == studentId);
        if (planner == null)
        {
            planner = new StudyPlanner
            {
                StudentId = studentId,
                ExamDate = DateTime.Today.AddDays(30),
                DailyStudyHours = 1.5m
            };
        }

        ViewData["Title"] = "Study Planner";
        var model = await BuildPlannerViewModel(studentId, planner);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(DateTime examDate, decimal dailyStudyHours)
    {
        if (examDate.Date <= DateTime.Today)
        {
            TempData["Error"] = "Choose a future exam date.";
            return RedirectToAction(nameof(Index));
        }

        if (dailyStudyHours < 0.5m || dailyStudyHours > 8m)
        {
            TempData["Error"] = "Daily study hours must be between 0.5 and 8.";
            return RedirectToAction(nameof(Index));
        }

        var studentId = GetStudentId();
        var planner = await _db.StudyPlanners.FirstOrDefaultAsync(p => p.StudentId == studentId);
        if (planner == null)
        {
            planner = new StudyPlanner { StudentId = studentId };
            _db.StudyPlanners.Add(planner);
        }

        planner.ExamDate = examDate.Date;
        planner.DailyStudyHours = dailyStudyHours;
        planner.LastGeneratedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Study plan updated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<StudyPlannerViewModel> BuildPlannerViewModel(Guid studentId, StudyPlanner planner)
    {
        var categories = await _db.Categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync();
        var weakAreas = await BuildWeakAreas(studentId);
        var categoryOrder = weakAreas.Any()
            ? weakAreas.Select(w => w.Category).Concat(categories.Select(c => c.Name)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            : categories.Select(c => c.Name).ToList();

        var lessons = await _db.Lessons.Include(l => l.Section).ThenInclude(s => s.Course).ThenInclude(c => c.Category)
            .Where(l => l.IsPublished)
            .OrderBy(l => l.Section.Course.Category.DisplayOrder).ThenBy(l => l.Section.Course.Title).ThenBy(l => l.Order)
            .ToListAsync();
        var practiceTests = await _db.PracticeTests.Include(t => t.Category).Include(t => t.Questions)
            .Where(t => t.IsPublished).OrderBy(t => t.Category.DisplayOrder).ThenBy(t => t.Title)
            .ToListAsync();
        var flashcardSets = await _db.FlashcardSets.Include(s => s.Category)
            .Where(s => s.IsPublished).OrderBy(s => s.Category.DisplayOrder).ThenBy(s => s.Title)
            .ToListAsync();
        var guides = await _db.StudyGuides.Include(g => g.Category)
            .Where(g => g.IsPublished).OrderBy(g => g.Category.DisplayOrder).ThenBy(g => g.Title)
            .ToListAsync();

        var daysUntilExam = Math.Max(1, (planner.ExamDate.Date - DateTime.Today).Days);
        var planDays = Math.Min(daysUntilExam, 14);
        var tasksPerDay = Math.Clamp((int)Math.Ceiling(planner.DailyStudyHours * 2), 2, 6);
        var days = new List<DailyStudyPlan>();

        for (var day = 0; day < planDays; day++)
        {
            var date = DateTime.Today.AddDays(day);
            var tasks = new List<StudyPlanTask>();
            for (var slot = 0; slot < tasksPerDay; slot++)
            {
                var categoryName = categoryOrder[(day + slot) % categoryOrder.Count];
                var type = slot % 4;
                StudyPlanTask? task = type switch
                {
                    0 => BuildLessonTask(categoryName, lessons, day + slot),
                    1 => BuildPracticeTask(categoryName, practiceTests, day + slot),
                    2 => BuildFlashcardTask(categoryName, flashcardSets, day + slot),
                    _ => BuildGuideTask(categoryName, guides, day + slot)
                };
                if (task != null) tasks.Add(task);
            }
            days.Add(new DailyStudyPlan(date, tasks));
        }

        var plannedHours = Math.Round(planDays * planner.DailyStudyHours, 1);
        var completionEstimate = Math.Min(100, Math.Round((decimal)planDays / daysUntilExam * 100m, 1));
        return new StudyPlannerViewModel(planner, days, weakAreas.Take(3).ToList(), daysUntilExam, plannedHours, completionEstimate);
    }

    private static StudyPlanTask? BuildLessonTask(string categoryName, List<Lesson> lessons, int offset)
    {
        var options = lessons.Where(l => SameCategory(l.Section.Course.Category.Name, categoryName)).ToList();
        var lesson = Pick(options, offset);
        return lesson == null ? null : new StudyPlanTask("Lesson", lesson.Title, lesson.Section.Course.Category.Name, $"{lesson.Section.Course.Title} - {lesson.DurationMinutes} min", $"/Lessons/Watch/{lesson.Id}");
    }

    private static StudyPlanTask? BuildPracticeTask(string categoryName, List<PracticeTest> tests, int offset)
    {
        var options = tests.Where(t => SameCategory(t.Category.Name, categoryName)).ToList();
        var test = Pick(options, offset);
        return test == null ? null : new StudyPlanTask("Practice Test", test.Title, test.Category.Name, $"{test.Questions.Count} questions", $"/PracticeTests/Take/{test.Id}");
    }

    private static StudyPlanTask? BuildFlashcardTask(string categoryName, List<FlashcardSet> sets, int offset)
    {
        var options = sets.Where(s => SameCategory(s.Category.Name, categoryName)).ToList();
        var set = Pick(options, offset);
        return set == null ? null : new StudyPlanTask("Flashcards", set.Title, set.Category.Name, "Review key terms", $"/Flashcards/Study/{set.Id}");
    }

    private static StudyPlanTask? BuildGuideTask(string categoryName, List<StudyGuide> guides, int offset)
    {
        var options = guides.Where(g => SameCategory(g.Category.Name, categoryName)).ToList();
        var guide = Pick(options, offset);
        return guide == null ? null : new StudyPlanTask("Study Guide", guide.Title, guide.Category.Name, "Read and bookmark notes", $"/StudyGuides/Read/{guide.Id}");
    }

    private async Task<List<WeakArea>> BuildWeakAreas(Guid studentId)
    {
        var scores = new Dictionary<string, List<decimal>>(StringComparer.OrdinalIgnoreCase);
        void Add(string? category, decimal score)
        {
            if (string.IsNullOrWhiteSpace(category)) return;
            if (!scores.TryGetValue(category, out var list)) scores[category] = list = new List<decimal>();
            list.Add(Math.Clamp(score, 0m, 100m));
        }

        var quizAttempts = await _db.QuizAttempts.Include(a => a.Quiz).ThenInclude(q => q.Lesson).ThenInclude(l => l.Section).ThenInclude(s => s.Course).ThenInclude(c => c.Category).Where(a => a.StudentId == studentId).ToListAsync();
        foreach (var attempt in quizAttempts) Add(attempt.Quiz?.Lesson?.Section?.Course?.Category?.Name, (decimal)attempt.Score);

        var practiceAttempts = await _db.PracticeTestAttempts.Include(a => a.PracticeTest).ThenInclude(t => t.Category).Where(a => a.StudentId == studentId).ToListAsync();
        foreach (var attempt in practiceAttempts) Add(attempt.PracticeTest?.Category?.Name, attempt.ScorePercent);

        var examAttempts = await _db.FullExamAttempts.Where(a => a.StudentId == studentId).OrderByDescending(a => a.CompletedAt).Take(3).ToListAsync();
        foreach (var attempt in examAttempts)
        {
            try
            {
                var breakdown = JsonSerializer.Deserialize<List<ExamBreakdownMetric>>(attempt.CategoryBreakdownJson) ?? new();
                foreach (var item in breakdown) Add(item.Category, item.ScorePercent);
            }
            catch (JsonException) { }
        }

        return scores.Select(s => new WeakArea(s.Key.Split(' ')[0], s.Key, Math.Round(s.Value.Average(), 1)))
            .OrderBy(w => w.AverageScore)
            .ToList();
    }

    private Guid GetStudentId()
    {
        var value = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(value!);
    }

    private static bool SameCategory(string left, string right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    private static T? Pick<T>(List<T> items, int offset) where T : class => items.Count == 0 ? null : items[offset % items.Count];

    public sealed record StudyPlannerViewModel(StudyPlanner Planner, List<DailyStudyPlan> Days, List<WeakArea> FocusAreas, int DaysUntilExam, decimal PlannedHours, decimal CompletionEstimate);
    public sealed record DailyStudyPlan(DateTime Date, List<StudyPlanTask> Tasks);
    public sealed record StudyPlanTask(string Type, string Title, string Category, string Detail, string Url);
    public sealed record WeakArea(string Code, string Category, decimal AverageScore);
    private sealed record ExamBreakdownMetric(string Category, int TotalQuestions, int CorrectAnswers, decimal ScorePercent);
}


