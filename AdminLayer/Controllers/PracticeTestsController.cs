using DataLayer.Context;
using DataLayer.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdminLayer.Controllers;

public class PracticeTestsController : Controller
{
    private readonly AppDbContext _db;
    public PracticeTestsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? categoryId)
    {
        var query = _db.PracticeTests.Include(t => t.Category).Include(t => t.Questions).AsQueryable();
        if (categoryId.HasValue) query = query.Where(t => t.CategoryId == categoryId.Value);
        ViewBag.Categories = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
        ViewBag.CategoryId = categoryId;
        return View(await query.OrderBy(t => t.Category.DisplayOrder).ThenBy(t => t.DisplayOrder).ToListAsync());
    }

    public async Task<IActionResult> Preview(Guid id)
    {
        var test = await _db.PracticeTests.Include(t => t.Category).Include(t => t.Questions.OrderBy(q => q.Order)).ThenInclude(q => q.QuestionBankQuestion).ThenInclude(q => q!.Category).FirstOrDefaultAsync(t => t.Id == id);
        if (test == null) return NotFound();
        return View(test);
    }

    public async Task<IActionResult> Create()
    {
        await LoadLookups();
        return View(new PracticeTest { IsPublished = true, IsTimed = true, TimeLimitMinutes = 15, PassingScorePercent = 70, Questions = DefaultQuestions() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PracticeTest test, List<Guid>? selectedBankQuestionIds)
    {
        await AddBankQuestions(test, selectedBankQuestionIds);
        Normalize(test);
        if (!ValidateTest(test)) { await LoadLookups(); return View(test); }
        test.Id = Guid.NewGuid();
        foreach (var q in test.Questions) { q.Id = Guid.NewGuid(); q.PracticeTestId = test.Id; }
        _db.PracticeTests.Add(test);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Practice test created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var test = await _db.PracticeTests.Include(t => t.Questions.OrderBy(q => q.Order)).FirstOrDefaultAsync(t => t.Id == id);
        if (test == null) return NotFound();
        await LoadLookups();
        return View(test);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, PracticeTest model, List<Guid>? selectedBankQuestionIds)
    {
        await AddBankQuestions(model, selectedBankQuestionIds);
        Normalize(model);
        if (!ValidateTest(model)) { model.Id = id; await LoadLookups(); return View(model); }
        var test = await _db.PracticeTests.Include(t => t.Questions).FirstOrDefaultAsync(t => t.Id == id);
        if (test == null) return NotFound();
        test.CategoryId = model.CategoryId;
        test.Title = model.Title;
        test.Description = model.Description;
        test.IsTimed = model.IsTimed;
        test.TimeLimitMinutes = model.TimeLimitMinutes;
        test.PassingScorePercent = model.PassingScorePercent;
        test.ShuffleQuestions = model.ShuffleQuestions;
        test.ShuffleAnswers = model.ShuffleAnswers;
        test.IsMockExam = model.IsMockExam;
        test.IsPublished = model.IsPublished;
        test.DisplayOrder = model.DisplayOrder;
        test.UpdatedAt = DateTime.UtcNow;
        _db.PracticeTestQuestions.RemoveRange(test.Questions);
        test.Questions = model.Questions.Select((q, i) => new PracticeTestQuestion
        {
            Id = Guid.NewGuid(),
            PracticeTestId = id,
            QuestionBankQuestionId = q.QuestionBankQuestionId,
            Text = q.Text,
            OptionA = q.OptionA,
            OptionB = q.OptionB,
            OptionC = q.OptionC,
            OptionD = q.OptionD,
            OptionE = q.OptionE,
            OptionF = q.OptionF,
            CorrectOption = q.CorrectOption,
            Explanation = q.Explanation,
            Order = i + 1
        }).ToList();
        await _db.SaveChangesAsync();
        TempData["Success"] = "Practice test updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duplicate(Guid id)
    {
        var source = await _db.PracticeTests.Include(t => t.Questions).AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
        if (source == null) return NotFound();
        var copy = new PracticeTest
        {
            Id = Guid.NewGuid(),
            CategoryId = source.CategoryId,
            Title = source.Title + " (Copy)",
            Description = source.Description,
            IsTimed = source.IsTimed,
            TimeLimitMinutes = source.TimeLimitMinutes,
            PassingScorePercent = source.PassingScorePercent,
            ShuffleQuestions = source.ShuffleQuestions,
            ShuffleAnswers = source.ShuffleAnswers,
            IsMockExam = source.IsMockExam,
            IsPublished = false,
            DisplayOrder = source.DisplayOrder,
            Questions = source.Questions.OrderBy(q => q.Order).Select(q => new PracticeTestQuestion
            {
                Id = Guid.NewGuid(),
                QuestionBankQuestionId = q.QuestionBankQuestionId,
                Text = q.Text,
                OptionA = q.OptionA,
                OptionB = q.OptionB,
                OptionC = q.OptionC,
                OptionD = q.OptionD,
                OptionE = q.OptionE,
                OptionF = q.OptionF,
                CorrectOption = q.CorrectOption,
                Explanation = q.Explanation,
                Order = q.Order
            }).ToList()
        };
        foreach (var question in copy.Questions) question.PracticeTestId = copy.Id;
        _db.PracticeTests.Add(copy);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Practice test duplicated as draft.";
        return RedirectToAction(nameof(Edit), new { id = copy.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Publish(Guid id) => SetPublished(id, true, "Practice test published.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Unpublish(Guid id) => SetPublished(id, false, "Practice test unpublished.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var test = await _db.PracticeTests.Include(t => t.Questions).Include(t => t.Attempts).FirstOrDefaultAsync(t => t.Id == id);
        if (test != null) { _db.PracticeTestAttempts.RemoveRange(test.Attempts); _db.PracticeTestQuestions.RemoveRange(test.Questions); _db.PracticeTests.Remove(test); await _db.SaveChangesAsync(); TempData["Success"] = "Practice test deleted."; }
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> SetPublished(Guid id, bool published, string message)
    {
        var test = await _db.PracticeTests.FirstOrDefaultAsync(t => t.Id == id);
        if (test == null) return NotFound();
        test.IsPublished = published;
        test.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = message;
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadLookups()
    {
        ViewBag.Categories = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
        ViewBag.BankQuestions = await _db.QuestionBankQuestions.Include(q => q.Category).Where(q => q.Status == "Published").OrderBy(q => q.Category.DisplayOrder).ThenBy(q => q.Topic).ThenBy(q => q.Difficulty).Take(500).ToListAsync();
    }

    private async Task AddBankQuestions(PracticeTest test, List<Guid>? selectedBankQuestionIds)
    {
        if (selectedBankQuestionIds == null || !selectedBankQuestionIds.Any()) return;
        test.Questions ??= new List<PracticeTestQuestion>();
        var existing = test.Questions.Where(q => q.QuestionBankQuestionId.HasValue).Select(q => q.QuestionBankQuestionId!.Value).ToHashSet();
        var bankQuestions = await _db.QuestionBankQuestions.Where(q => selectedBankQuestionIds.Contains(q.Id)).ToListAsync();
        foreach (var q in bankQuestions.Where(q => !existing.Contains(q.Id)))
        {
            test.Questions.Add(new PracticeTestQuestion { QuestionBankQuestionId = q.Id, Text = q.Text, OptionA = q.OptionA, OptionB = q.OptionB, OptionC = q.OptionC, OptionD = q.OptionD, OptionE = q.OptionE, OptionF = q.OptionF, CorrectOption = q.CorrectOption, Explanation = q.Explanation, Order = test.Questions.Count + 1 });
        }
    }

    private static List<PracticeTestQuestion> DefaultQuestions() => Enumerable.Range(1, 3).Select(i => new PracticeTestQuestion { Order = i, CorrectOption = "A" }).ToList();
    private static void Normalize(PracticeTest test)
    {
        test.Questions = test.Questions.Where(q => !string.IsNullOrWhiteSpace(q.Text)).Select((q, i) => { q.Order = i + 1; q.CorrectOption = string.IsNullOrWhiteSpace(q.CorrectOption) ? "A" : q.CorrectOption[..1].ToUpperInvariant(); return q; }).ToList();
        if (!test.IsTimed) test.TimeLimitMinutes = null;
        test.PassingScorePercent = Math.Clamp(test.PassingScorePercent <= 0 ? 70 : test.PassingScorePercent, 1, 100);
    }
    private bool ValidateTest(PracticeTest test) { if (test.CategoryId <= 0) ModelState.AddModelError(nameof(test.CategoryId), "Category is required."); if (string.IsNullOrWhiteSpace(test.Title)) ModelState.AddModelError(nameof(test.Title), "Title is required."); if (test.IsTimed && (!test.TimeLimitMinutes.HasValue || test.TimeLimitMinutes <= 0)) ModelState.AddModelError(nameof(test.TimeLimitMinutes), "Timed tests need a time limit."); if (!test.Questions.Any()) ModelState.AddModelError("Questions", "At least one question is required."); ModelState.Remove(nameof(test.Category)); ModelState.Remove(nameof(test.Attempts)); return ModelState.IsValid; }
}
