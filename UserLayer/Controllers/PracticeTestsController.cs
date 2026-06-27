using System.Security.Claims;
using System.Text.Json;
using DataLayer.Context;
using DataLayer.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UserLayer.Controllers;

public class PracticeTestsController : Controller
{
    private readonly AppDbContext _db;
    public PracticeTestsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? categoryId)
    {
        ViewBag.Categories = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
        ViewBag.CategoryId = categoryId;
        var query = _db.PracticeTests.Include(t => t.Category).Include(t => t.Questions).Where(t => t.IsPublished);
        if (categoryId.HasValue) query = query.Where(t => t.CategoryId == categoryId.Value);
        ViewData["Title"] = "ASVAB Practice Tests";
        return View(await query.OrderBy(t => t.Category.DisplayOrder).ThenBy(t => t.DisplayOrder).ToListAsync());
    }

    public async Task<IActionResult> Take(Guid id)
    {
        var test = await _db.PracticeTests.Include(t => t.Category).Include(t => t.Questions.OrderBy(q => q.Order)).FirstOrDefaultAsync(t => t.Id == id && t.IsPublished);
        if (test == null) return NotFound();
        ViewData["Title"] = test.Title;
        return View(test);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid id, Dictionary<Guid, string> answers)
    {
        var test = await _db.PracticeTests.Include(t => t.Questions).FirstOrDefaultAsync(t => t.Id == id && t.IsPublished);
        if (test == null) return NotFound();
        var total = test.Questions.Count;
        var correct = test.Questions.Count(q => answers.TryGetValue(q.Id, out var selected) && string.Equals(selected, q.CorrectOption, StringComparison.OrdinalIgnoreCase));
        var score = total == 0 ? 0 : Math.Round((decimal)correct * 100m / total, 2);
        var attempt = new PracticeTestAttempt { PracticeTestId = id, StudentId = TryGetCurrentUserId(out var userId) ? userId : null, StartedAt = DateTime.UtcNow, CompletedAt = DateTime.UtcNow, TotalQuestions = total, CorrectAnswers = correct, ScorePercent = score, AnswersJson = JsonSerializer.Serialize(answers.ToDictionary(a => a.Key.ToString(), a => a.Value)) };
        _db.PracticeTestAttempts.Add(attempt);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Results), new { id = attempt.Id });
    }

    public async Task<IActionResult> Results(Guid id)
    {
        var attempt = await _db.PracticeTestAttempts.Include(a => a.PracticeTest).ThenInclude(t => t.Category).Include(a => a.PracticeTest).ThenInclude(t => t.Questions.OrderBy(q => q.Order)).FirstOrDefaultAsync(a => a.Id == id);
        if (attempt == null) return NotFound();
        ViewData["Title"] = "Practice Test Results";
        return View(attempt);
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var value = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out userId);
    }
}
