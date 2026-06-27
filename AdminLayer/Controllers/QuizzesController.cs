using AdminLayer.Models;
using DataLayer.Context;
using DataLayer.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SharedLayer.Enums;

namespace AdminLayer.Controllers;

public class QuizzesController : Controller
{
    private static readonly string[] Difficulties = ["Easy", "Medium", "Hard"];
    private readonly AppDbContext _db;

    public QuizzesController(AppDbContext db) => _db = db;

    [HttpGet("Quizzes")]
    public async Task<IActionResult> All(string? search, bool? isPublished)
    {
        var query = _db.Quizzes
            .Include(q => q.Lesson).ThenInclude(l => l.Section).ThenInclude(s => s.Course).ThenInclude(c => c.Category)
            .Include(q => q.Questions)
            .Include(q => q.Attempts)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(q => q.Title.Contains(term) || q.Lesson.Title.Contains(term) || q.Lesson.Section.Course.Title.Contains(term));
        }
        if (isPublished.HasValue) query = query.Where(q => q.IsPublished == isPublished.Value);

        ViewData["Title"] = "Quizzes";
        return View(new QuizIndexViewModel
        {
            Quizzes = await query.OrderBy(q => q.Lesson.Section.Course.Title).ThenBy(q => q.Lesson.Order).ToListAsync(),
            Lessons = await AvailableLessons(null),
            Categories = await Categories(),
            Search = search,
            IsPublished = isPublished
        });
    }

    [HttpGet("Quizzes/Index/{lessonId:guid}")]
    public async Task<IActionResult> Index(Guid lessonId)
    {
        var quizzes = await _db.Quizzes
            .Include(q => q.Lesson).ThenInclude(l => l.Section).ThenInclude(s => s.Course)
            .Include(q => q.Questions)
            .Include(q => q.Attempts)
            .Where(q => q.LessonId == lessonId)
            .ToListAsync();
        ViewBag.LessonId = lessonId;
        return View(quizzes);
    }

    [HttpGet("Quizzes/Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var quiz = await LoadQuiz(id);
        if (quiz == null) return NotFound();
        ViewBag.Stats = new
        {
            Attempts = quiz.Attempts.Count,
            PassRate = quiz.Attempts.Count == 0 ? 0 : Math.Round(quiz.Attempts.Count(a => a.IsPassed) * 100.0 / quiz.Attempts.Count, 1),
            AverageScore = quiz.Attempts.Count == 0 ? 0 : Math.Round(quiz.Attempts.Average(a => a.Score), 1)
        };
        return View(quiz);
    }

    [HttpGet("Quizzes/Create")]
    public async Task<IActionResult> Create()
    {
        var model = DefaultModel(Guid.Empty);
        await LoadLookups(null);
        return View(model);
    }

    [HttpGet("Quizzes/Create/{lessonId:guid}")]
    public async Task<IActionResult> Create(Guid lessonId)
    {
        var model = DefaultModel(lessonId);
        await LoadLookups(null);
        return View(model);
    }

    [HttpPost("Quizzes/Create")]
    [HttpPost("Quizzes/Create/{lessonId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid? lessonId, QuizCreateViewModel model, List<Guid>? selectedBankQuestionIds)
    {
        if (lessonId.HasValue && lessonId.Value != Guid.Empty) model.LessonId = lessonId.Value;
        await AddBankQuestions(model, selectedBankQuestionIds);
        NormalizeModel(model);
        ValidateModel(model, null);
        if (!ModelState.IsValid)
        {
            await LoadLookups(null);
            return View(model);
        }

        var quiz = ToQuiz(model, Guid.NewGuid());
        _db.Quizzes.Add(quiz);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Quiz created.";
        return RedirectToAction(nameof(Details), new { id = quiz.Id });
    }

    [HttpGet("Quizzes/Edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var quiz = await LoadQuiz(id);
        if (quiz == null) return NotFound();
        await LoadLookups(id);
        return View("Create", ToModel(quiz));
    }

    [HttpPost("Quizzes/Edit/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, QuizCreateViewModel model, List<Guid>? selectedBankQuestionIds)
    {
        model.Id = id;
        await AddBankQuestions(model, selectedBankQuestionIds);
        NormalizeModel(model);
        ValidateModel(model, id);
        if (!ModelState.IsValid)
        {
            await LoadLookups(id);
            return View("Create", model);
        }

        var quiz = await _db.Quizzes.Include(q => q.Questions).ThenInclude(q => q.Answers).FirstOrDefaultAsync(q => q.Id == id);
        if (quiz == null) return NotFound();
        quiz.LessonId = model.LessonId;
        quiz.Title = model.Title.Trim();
        quiz.Description = model.Description?.Trim();
        quiz.PassingScore = Math.Clamp(model.PassingScore, 0, 100);
        quiz.TimeLimitMinutes = model.TimeLimitMinutes > 0 ? model.TimeLimitMinutes : null;
        quiz.MaxAttempts = Math.Max(0, model.MaxAttempts);
        quiz.ShuffleQuestions = model.ShuffleQuestions;
        quiz.ShuffleAnswers = model.ShuffleAnswers;
        quiz.ShowAnswersAfterSubmission = model.ShowAnswersAfterSubmission;
        quiz.IsPublished = model.IsPublished;
        quiz.UpdatedAt = DateTime.UtcNow;

        _db.Questions.RemoveRange(quiz.Questions);
        quiz.Questions = BuildQuestions(model, quiz.Id);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Quiz updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("Quizzes/Delete/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid? lessonId)
    {
        var quiz = await _db.Quizzes.Include(q => q.Questions).ThenInclude(q => q.Answers).Include(q => q.Attempts).FirstOrDefaultAsync(q => q.Id == id);
        if (quiz != null)
        {
            _db.QuizAttempts.RemoveRange(quiz.Attempts);
            _db.Quizzes.Remove(quiz);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Quiz deleted.";
        }
        return lessonId.HasValue && lessonId.Value != Guid.Empty ? RedirectToAction(nameof(Index), new { lessonId }) : RedirectToAction(nameof(All));
    }

    [HttpPost("Quizzes/Publish/{id:guid}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Publish(Guid id) => SetPublished(id, true, "Quiz published.");

    [HttpPost("Quizzes/Unpublish/{id:guid}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Unpublish(Guid id) => SetPublished(id, false, "Quiz unpublished.");

    [HttpPost("Quizzes/DuplicateQuestion/{id:guid}/{questionId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DuplicateQuestion(Guid id, Guid questionId)
    {
        var quiz = await LoadQuiz(id);
        var source = quiz?.Questions.FirstOrDefault(q => q.Id == questionId);
        if (quiz == null || source == null) return NotFound();
        var copy = new Question
        {
            Id = Guid.NewGuid(),
            QuizId = id,
            QuestionBankQuestionId = source.QuestionBankQuestionId,
            CategoryId = source.CategoryId,
            Text = source.Text + " (Copy)",
            ImageUrl = source.ImageUrl,
            Explanation = source.Explanation,
            Difficulty = source.Difficulty,
            Type = QuestionType.MultipleChoice,
            Points = source.Points,
            Order = quiz.Questions.Count + 1,
            Answers = source.Answers.OrderBy(a => a.Order).Select(a => new Answer { Id = Guid.NewGuid(), Text = a.Text, IsCorrect = a.IsCorrect, Order = a.Order }).ToList()
        };
        _db.Questions.Add(copy);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Question duplicated.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("Quizzes/DeleteQuestion/{id:guid}/{questionId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(Guid id, Guid questionId)
    {
        var question = await _db.Questions.FirstOrDefaultAsync(q => q.Id == questionId && q.QuizId == id);
        if (question == null) return NotFound();
        _db.Questions.Remove(question);
        await _db.SaveChangesAsync();
        await Resequence(id);
        TempData["Success"] = "Question removed.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("Quizzes/MoveQuestion/{id:guid}/{questionId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveQuestion(Guid id, Guid questionId, string direction)
    {
        var questions = await _db.Questions.Where(q => q.QuizId == id).OrderBy(q => q.Order).ToListAsync();
        var index = questions.FindIndex(q => q.Id == questionId);
        var swapIndex = direction == "up" ? index - 1 : index + 1;
        if (index >= 0 && swapIndex >= 0 && swapIndex < questions.Count)
        {
            (questions[index].Order, questions[swapIndex].Order) = (questions[swapIndex].Order, questions[index].Order);
            await _db.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Edit), new { id });
    }

    private async Task<IActionResult> SetPublished(Guid id, bool published, string message)
    {
        var quiz = await _db.Quizzes.FirstOrDefaultAsync(q => q.Id == id);
        if (quiz == null) return NotFound();
        quiz.IsPublished = published;
        quiz.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = message;
        return RedirectToAction(nameof(All));
    }

    private async Task<Quiz?> LoadQuiz(Guid id) => await _db.Quizzes
        .Include(q => q.Lesson).ThenInclude(l => l.Section).ThenInclude(s => s.Course).ThenInclude(c => c.Category)
        .Include(q => q.Questions.OrderBy(q => q.Order)).ThenInclude(q => q.Answers.OrderBy(a => a.Order))
        .Include(q => q.Questions).ThenInclude(q => q.Category)
        .Include(q => q.Attempts)
        .FirstOrDefaultAsync(q => q.Id == id);

    private async Task LoadLookups(Guid? currentQuizId)
    {
        ViewBag.Lessons = await AvailableLessons(currentQuizId);
        ViewBag.Categories = await Categories();
        ViewBag.Difficulties = Difficulties;
        ViewBag.BankQuestions = await _db.QuestionBankQuestions.Include(q => q.Category).Where(q => q.Status == "Published").OrderBy(q => q.Category.DisplayOrder).ThenBy(q => q.Topic).ThenBy(q => q.Difficulty).Take(500).ToListAsync();
    }

    private async Task<List<Lesson>> AvailableLessons(Guid? currentQuizId)
    {
        var usedLessonIds = await _db.Quizzes.Where(q => !currentQuizId.HasValue || q.Id != currentQuizId.Value).Select(q => q.LessonId).ToListAsync();
        return await _db.Lessons.Include(l => l.Section).ThenInclude(s => s.Course).Where(l => !usedLessonIds.Contains(l.Id)).OrderBy(l => l.Section.Course.Title).ThenBy(l => l.Order).ToListAsync();
    }

    private Task<List<Category>> Categories() => _db.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync();

    private async Task AddBankQuestions(QuizCreateViewModel model, List<Guid>? selectedBankQuestionIds)
    {
        if (selectedBankQuestionIds == null || !selectedBankQuestionIds.Any()) return;
        var existingBankIds = model.Questions.Where(q => q.QuestionBankQuestionId.HasValue).Select(q => q.QuestionBankQuestionId!.Value).ToHashSet();
        var bank = await _db.QuestionBankQuestions.Include(q => q.Category).Where(q => selectedBankQuestionIds.Contains(q.Id)).ToListAsync();
        foreach (var q in bank.Where(q => !existingBankIds.Contains(q.Id)))
        {
            model.Questions.Add(new QuizQuestionInputModel
            {
                QuestionBankQuestionId = q.Id,
                CategoryId = q.CategoryId,
                Difficulty = q.Difficulty,
                Text = q.Text,
                ImageUrl = q.QuestionImageUrl,
                Explanation = q.Explanation,
                Points = 1,
                CorrectAnswerIndex = Math.Max(0, "ABCDEF".IndexOf(q.CorrectOption, StringComparison.OrdinalIgnoreCase)),
                Answers = new List<string> { q.OptionA, q.OptionB, q.OptionC, q.OptionD, q.OptionE ?? string.Empty, q.OptionF ?? string.Empty }
            });
        }
    }

    private static QuizCreateViewModel DefaultModel(Guid lessonId) => new()
    {
        LessonId = lessonId,
        PassingScore = 70,
        ShowAnswersAfterSubmission = true,
        IsPublished = true,
        Questions = Enumerable.Range(0, 3).Select(i => new QuizQuestionInputModel { Order = i + 1, Points = 1, Answers = new List<string> { "", "", "", "", "", "" } }).ToList()
    };

    private static void NormalizeModel(QuizCreateViewModel model)
    {
        model.Title = model.Title?.Trim() ?? string.Empty;
        model.Description = model.Description?.Trim();
        model.PassingScore = Math.Clamp(model.PassingScore, 0, 100);
        model.MaxAttempts = Math.Max(0, model.MaxAttempts);
        model.TimeLimitMinutes = model.TimeLimitMinutes > 0 ? model.TimeLimitMinutes : null;
        foreach (var question in model.Questions)
        {
            question.Text = question.Text?.Trim() ?? string.Empty;
            question.Explanation = question.Explanation?.Trim();
            question.ImageUrl = question.ImageUrl?.Trim();
            question.Difficulty = Difficulties.FirstOrDefault(d => d.Equals(question.Difficulty, StringComparison.OrdinalIgnoreCase)) ?? "Easy";
            while (question.Answers.Count < 6) question.Answers.Add(string.Empty);
            question.Answers = question.Answers.Take(6).Select(a => a?.Trim() ?? string.Empty).ToList();
        }
        model.Questions = model.Questions.Where(q => !string.IsNullOrWhiteSpace(q.Text)).Select((q, index) => { q.Order = index + 1; return q; }).ToList();
    }

    private void ValidateModel(QuizCreateViewModel model, Guid? currentQuizId)
    {
        if (model.LessonId == Guid.Empty) ModelState.AddModelError(nameof(model.LessonId), "Lesson is required.");
        if (string.IsNullOrWhiteSpace(model.Title)) ModelState.AddModelError(nameof(model.Title), "Title is required.");
        if (!model.Questions.Any()) ModelState.AddModelError("Questions", "Add at least one question.");
        if (_db.Quizzes.Any(q => q.LessonId == model.LessonId && (!currentQuizId.HasValue || q.Id != currentQuizId.Value))) ModelState.AddModelError(nameof(model.LessonId), "A quiz already exists for this lesson.");
        for (var i = 0; i < model.Questions.Count; i++)
        {
            var question = model.Questions[i];
            var answers = question.Answers.Where(a => !string.IsNullOrWhiteSpace(a)).ToList();
            if (answers.Count < 2) ModelState.AddModelError($"Questions[{i}].Answers", $"Question {i + 1} needs at least two answers.");
            if (question.CorrectAnswerIndex < 0 || question.CorrectAnswerIndex >= question.Answers.Count || string.IsNullOrWhiteSpace(question.Answers[question.CorrectAnswerIndex])) ModelState.AddModelError($"Questions[{i}].CorrectAnswerIndex", $"Question {i + 1} needs a correct populated answer.");
        }
    }

    private static Quiz ToQuiz(QuizCreateViewModel model, Guid quizId) => new()
    {
        Id = quizId,
        LessonId = model.LessonId,
        Title = model.Title.Trim(),
        Description = model.Description?.Trim(),
        PassingScore = model.PassingScore,
        TimeLimitMinutes = model.TimeLimitMinutes,
        MaxAttempts = model.MaxAttempts,
        ShuffleQuestions = model.ShuffleQuestions,
        ShuffleAnswers = model.ShuffleAnswers,
        ShowAnswersAfterSubmission = model.ShowAnswersAfterSubmission,
        IsPublished = model.IsPublished,
        Questions = BuildQuestions(model, quizId)
    };

    private static List<Question> BuildQuestions(QuizCreateViewModel model, Guid quizId) => model.Questions.Select((q, qi) => new Question
    {
        Id = q.Id == Guid.Empty ? Guid.NewGuid() : q.Id,
        QuizId = quizId,
        QuestionBankQuestionId = q.QuestionBankQuestionId,
        CategoryId = q.CategoryId,
        Text = q.Text,
        ImageUrl = string.IsNullOrWhiteSpace(q.ImageUrl) ? null : q.ImageUrl,
        Explanation = q.Explanation,
        Difficulty = q.Difficulty,
        Type = QuestionType.MultipleChoice,
        Points = Math.Max(1, q.Points),
        Order = qi + 1,
        Answers = q.Answers.Select((answer, ai) => new { answer, ai }).Where(x => !string.IsNullOrWhiteSpace(x.answer)).Select(x => new Answer { Id = Guid.NewGuid(), Text = x.answer, IsCorrect = x.ai == q.CorrectAnswerIndex, Order = x.ai + 1 }).ToList()
    }).ToList();

    private static QuizCreateViewModel ToModel(Quiz quiz) => new()
    {
        Id = quiz.Id,
        LessonId = quiz.LessonId,
        Title = quiz.Title,
        Description = quiz.Description,
        PassingScore = quiz.PassingScore,
        TimeLimitMinutes = quiz.TimeLimitMinutes,
        MaxAttempts = quiz.MaxAttempts,
        ShuffleQuestions = quiz.ShuffleQuestions,
        ShuffleAnswers = quiz.ShuffleAnswers,
        ShowAnswersAfterSubmission = quiz.ShowAnswersAfterSubmission,
        IsPublished = quiz.IsPublished,
        Questions = quiz.Questions.OrderBy(q => q.Order).Select(q =>
        {
            var answers = q.Answers.OrderBy(a => a.Order).ToList();
            var values = answers.Select(a => a.Text).Concat(new[] { "", "", "", "", "", "" }).Take(6).ToList();
            return new QuizQuestionInputModel
            {
                Id = q.Id,
                QuestionBankQuestionId = q.QuestionBankQuestionId,
                CategoryId = q.CategoryId,
                Difficulty = q.Difficulty,
                Text = q.Text,
                ImageUrl = q.ImageUrl,
                Explanation = q.Explanation,
                Points = q.Points,
                Order = q.Order,
                CorrectAnswerIndex = Math.Max(0, answers.FindIndex(a => a.IsCorrect)),
                Answers = values
            };
        }).ToList()
    };

    private async Task Resequence(Guid quizId)
    {
        var questions = await _db.Questions.Where(q => q.QuizId == quizId).OrderBy(q => q.Order).ToListAsync();
        for (var i = 0; i < questions.Count; i++) questions[i].Order = i + 1;
        await _db.SaveChangesAsync();
    }
}
