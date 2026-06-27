using System.Globalization;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Xml.Linq;
using AdminLayer.Models;
using DataLayer.Context;
using DataLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdminLayer.Controllers;

public class QuestionBankController : Controller
{
    private static readonly string[] Difficulties = ["Easy", "Medium", "Hard"];
    private static readonly string[] Statuses = ["Draft", "Published", "Archived"];
    private readonly AppDbContext _db;

    public QuestionBankController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? search, int? categoryId, string? topic, string? difficulty, string? status, string? tag, string sort = "newest")
    {
        var query = _db.QuestionBankQuestions.Include(q => q.Category).Include(q => q.PracticeTestQuestions).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(q => EF.Functions.Like(q.Text, term) || EF.Functions.Like(q.Topic, term) || EF.Functions.Like(q.Subtopic, term) || EF.Functions.Like(q.Tags, term) || EF.Functions.Like(q.Category.Name, term));
        }
        if (categoryId.HasValue) query = query.Where(q => q.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(topic)) query = query.Where(q => q.Topic == topic);
        if (!string.IsNullOrWhiteSpace(difficulty)) query = query.Where(q => q.Difficulty == difficulty);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(q => q.Status == status);
        if (!string.IsNullOrWhiteSpace(tag))
        {
            var tagTerm = $"%{tag.Trim()}%";
            query = query.Where(q => EF.Functions.Like(q.Tags, tagTerm));
        }

        query = sort switch
        {
            "oldest" => query.OrderBy(q => q.CreatedAt),
            "most-used" => query.OrderByDescending(q => q.PracticeTestQuestions.Count),
            "least-used" => query.OrderBy(q => q.PracticeTestQuestions.Count),
            _ => query.OrderByDescending(q => q.CreatedAt)
        };

        var model = new QuestionBankIndexViewModel
        {
            Questions = await query.Take(300).ToListAsync(),
            Categories = await Categories(),
            Search = search,
            CategoryId = categoryId,
            Topic = topic,
            Difficulty = difficulty,
            Status = status,
            Tag = tag,
            Sort = sort,
            TotalCount = await _db.QuestionBankQuestions.CountAsync(),
            DraftCount = await _db.QuestionBankQuestions.CountAsync(q => q.Status == "Draft"),
            PublishedCount = await _db.QuestionBankQuestions.CountAsync(q => q.Status == "Published"),
            ArchivedCount = await _db.QuestionBankQuestions.CountAsync(q => q.Status == "Archived")
        };

        ViewBag.Topics = await _db.QuestionBankQuestions.Where(q => q.Topic != "").Select(q => q.Topic).Distinct().OrderBy(t => t).ToListAsync();
        ViewData["Title"] = "Question Bank";
        return View(model);
    }

    public async Task<IActionResult> Create()
    {
        await LoadLookups();
        ViewData["Title"] = "Create Question";
        return View(new QuestionBankQuestion { Difficulty = "Easy", Status = "Draft", EstimatedTimeSeconds = 60 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(QuestionBankQuestion question)
    {
        Normalize(question, isNew: true);
        ValidateQuestion(question);
        if (!ModelState.IsValid)
        {
            await LoadLookups();
            return View(question);
        }

        question.Id = Guid.NewGuid();
        question.CreatedBy = CurrentUserName();
        question.ModifiedBy = CurrentUserName();
        _db.QuestionBankQuestions.Add(question);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Question created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var question = await _db.QuestionBankQuestions.FirstOrDefaultAsync(q => q.Id == id);
        if (question == null) return NotFound();
        await LoadLookups();
        ViewData["Title"] = "Edit Question";
        return View(question);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, QuestionBankQuestion model)
    {
        Normalize(model, isNew: false);
        ValidateQuestion(model);
        if (!ModelState.IsValid)
        {
            model.Id = id;
            await LoadLookups();
            return View(model);
        }

        var question = await _db.QuestionBankQuestions.FirstOrDefaultAsync(q => q.Id == id);
        if (question == null) return NotFound();

        question.CategoryId = model.CategoryId;
        question.Topic = model.Topic;
        question.Subtopic = model.Subtopic;
        question.Difficulty = model.Difficulty;
        question.Text = model.Text;
        question.QuestionImageUrl = model.QuestionImageUrl;
        question.ExplanationImageUrl = model.ExplanationImageUrl;
        question.OptionA = model.OptionA;
        question.OptionB = model.OptionB;
        question.OptionC = model.OptionC;
        question.OptionD = model.OptionD;
        question.OptionE = model.OptionE;
        question.OptionF = model.OptionF;
        question.CorrectOption = model.CorrectOption;
        question.Explanation = model.Explanation;
        question.WrongAnswerExplanation = model.WrongAnswerExplanation;
        question.SourceReference = model.SourceReference;
        question.Tags = model.Tags;
        question.EstimatedTimeSeconds = model.EstimatedTimeSeconds;
        question.Status = model.Status;
        question.DisplayOrder = model.DisplayOrder;
        question.ModifiedBy = CurrentUserName();
        question.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Question updated.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Preview(Guid id)
    {
        var question = await _db.QuestionBankQuestions.Include(q => q.Category).Include(q => q.PracticeTestQuestions).FirstOrDefaultAsync(q => q.Id == id);
        if (question == null) return NotFound();
        ViewData["Title"] = "Preview Question";
        return View(question);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Duplicate(Guid id)
    {
        var source = await _db.QuestionBankQuestions.AsNoTracking().FirstOrDefaultAsync(q => q.Id == id);
        if (source == null) return NotFound();
        source.Id = Guid.NewGuid();
        source.Status = "Draft";
        source.CreatedAt = DateTime.UtcNow;
        source.UpdatedAt = DateTime.UtcNow;
        source.CreatedBy = CurrentUserName();
        source.ModifiedBy = CurrentUserName();
        source.Text = $"{source.Text} (Copy)";
        _db.QuestionBankQuestions.Add(source);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Question duplicated as a draft.";
        return RedirectToAction(nameof(Edit), new { id = source.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Archive(Guid id) => UpdateStatus(id, "Archived", "Question archived.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Restore(Guid id) => UpdateStatus(id, "Draft", "Question restored as a draft.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Publish(Guid id) => UpdateStatus(id, "Published", "Question published.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Unpublish(Guid id) => UpdateStatus(id, "Draft", "Question unpublished.");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var question = await _db.QuestionBankQuestions.Include(q => q.PracticeTestQuestions).FirstOrDefaultAsync(q => q.Id == id);
        if (question == null) return NotFound();

        var usedByQuiz = await _db.Questions.AnyAsync(q => q.QuestionBankQuestionId == id);
        if (question.PracticeTestQuestions.Any() || usedByQuiz)
        {
            question.Status = "Archived";
            question.ModifiedBy = CurrentUserName();
            question.UpdatedAt = DateTime.UtcNow;
            TempData["Success"] = "Question is in use, so it was archived instead of deleted.";
        }
        else
        {
            _db.QuestionBankQuestions.Remove(question);
            TempData["Success"] = "Question deleted.";
        }

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Import()
    {
        ViewData["Title"] = "Import Questions";
        return View(new QuestionBankImportViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(QuestionBankImportViewModel model)
    {
        if (model.File == null || model.File.Length == 0)
        {
            model.Errors.Add("Choose a CSV or Excel .xlsx file.");
            return View(model);
        }

        var rows = model.File.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
            ? ReadXlsxRows(model.File)
            : ReadCsvRows(model.File);

        var categories = await Categories();
        var imported = new List<QuestionBankQuestion>();
        var rowNumber = 1;
        foreach (var row in rows)
        {
            rowNumber++;
            var question = BuildQuestionFromRow(row, categories, model.Errors, rowNumber);
            if (question != null) imported.Add(question);
        }

        if (!model.Errors.Any() && imported.Any() && model.SaveValidRows)
        {
            _db.QuestionBankQuestions.AddRange(imported);
            await _db.SaveChangesAsync();
            model.ImportedCount = imported.Count;
            TempData["Success"] = $"Imported {imported.Count} questions.";
            return RedirectToAction(nameof(Index));
        }

        if (!imported.Any() && !model.Errors.Any()) model.Errors.Add("No question rows were found in the file.");
        return View(model);
    }

    public async Task<IActionResult> Export(string format = "csv")
    {
        var questions = await _db.QuestionBankQuestions.Include(q => q.Category).OrderBy(q => q.Category.DisplayOrder).ThenBy(q => q.Topic).ThenBy(q => q.DisplayOrder).ToListAsync();
        var rows = ExportRows(questions);
        if (format.Equals("xlsx", StringComparison.OrdinalIgnoreCase))
        {
            var bytes = BuildXlsx(rows);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "question-bank.xlsx");
        }

        return File(Encoding.UTF8.GetBytes(BuildCsv(rows)), "text/csv", "question-bank.csv");
    }

    public async Task<IActionResult> Builder()
    {
        ViewData["Title"] = "Test Builder";
        return View(new QuestionBankTestBuilderViewModel { Categories = await Categories() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Builder(QuestionBankTestBuilderViewModel model)
    {
        model.Categories = await Categories();
        var questions = new List<QuestionBankQuestion>();
        await AddRandomQuestions(model.CategoryId, "Easy", model.EasyCount, questions, model.Errors);
        await AddRandomQuestions(model.CategoryId, "Medium", model.MediumCount, questions, model.Errors);
        await AddRandomQuestions(model.CategoryId, "Hard", model.HardCount, questions, model.Errors);

        if (string.IsNullOrWhiteSpace(model.Title)) model.Errors.Add("Title is required.");
        if (!questions.Any()) model.Errors.Add("Select at least one question.");
        if (model.Errors.Any()) return View(model);

        var test = BuildPracticeTest(model.Title, model.Description, model.CategoryId, questions, model.TimeLimitMinutes, model.PassingScorePercent, model.ShuffleQuestions, model.ShuffleAnswers, false);
        _db.PracticeTests.Add(test);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Practice test generated from the question bank.";
        return RedirectToAction("Edit", "PracticeTests", new { id = test.Id });
    }

    public async Task<IActionResult> MockExamBuilder()
    {
        var categories = await Categories();
        var model = new MockExamBuilderViewModel { Categories = categories };
        foreach (var category in categories) model.CategoryQuestionCounts[category.Id] = 9;
        ViewData["Title"] = "Mock Exam Builder";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MockExamBuilder(MockExamBuilderViewModel model)
    {
        model.Categories = await Categories();
        var questions = new List<QuestionBankQuestion>();
        foreach (var category in model.Categories)
        {
            var count = model.CategoryQuestionCounts.TryGetValue(category.Id, out var value) ? value : 0;
            if (count <= 0) continue;
            var pool = await PublishedQuestions(category.Id).ToListAsync();
            if (pool.Count < count) model.Errors.Add($"{category.Name} needs {count} questions, but only {pool.Count} published questions are available.");
            else questions.AddRange(pool.OrderBy(_ => Guid.NewGuid()).Take(count));
        }

        if (string.IsNullOrWhiteSpace(model.Title)) model.Errors.Add("Title is required.");
        if (!questions.Any()) model.Errors.Add("Add at least one category question count.");
        if (model.Errors.Any()) return View(model);

        var primaryCategoryId = questions.First().CategoryId;
        var test = BuildPracticeTest(model.Title, model.Description, primaryCategoryId, questions, model.TimeLimitMinutes, model.PassingScorePercent, model.ShuffleQuestions, model.ShuffleAnswers, true);
        _db.PracticeTests.Add(test);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Mock exam generated from the question bank.";
        return RedirectToAction("Edit", "PracticeTests", new { id = test.Id });
    }

    private async Task<IActionResult> UpdateStatus(Guid id, string status, string message)
    {
        var question = await _db.QuestionBankQuestions.FirstOrDefaultAsync(q => q.Id == id);
        if (question == null) return NotFound();
        question.Status = status;
        question.ModifiedBy = CurrentUserName();
        question.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = message;
        return RedirectToAction(nameof(Index));
    }

    private IQueryable<QuestionBankQuestion> PublishedQuestions(int categoryId) => _db.QuestionBankQuestions.Where(q => q.CategoryId == categoryId && q.Status == "Published");

    private async Task AddRandomQuestions(int categoryId, string difficulty, int count, List<QuestionBankQuestion> questions, List<string> errors)
    {
        if (count <= 0) return;
        var pool = await PublishedQuestions(categoryId).Where(q => q.Difficulty == difficulty).ToListAsync();
        if (pool.Count < count) errors.Add($"{difficulty} requires {count} questions, but only {pool.Count} published questions are available.");
        else questions.AddRange(pool.OrderBy(_ => Guid.NewGuid()).Take(count));
    }

    private static PracticeTest BuildPracticeTest(string title, string description, int categoryId, List<QuestionBankQuestion> questions, int timeLimitMinutes, int passingScore, bool shuffleQuestions, bool shuffleAnswers, bool isMockExam)
    {
        if (shuffleQuestions) questions = questions.OrderBy(_ => Guid.NewGuid()).ToList();
        var test = new PracticeTest
        {
            Id = Guid.NewGuid(),
            CategoryId = categoryId,
            Title = title.Trim(),
            Description = description?.Trim() ?? string.Empty,
            IsTimed = timeLimitMinutes > 0,
            TimeLimitMinutes = timeLimitMinutes > 0 ? timeLimitMinutes : null,
            PassingScorePercent = Math.Clamp(passingScore, 1, 100),
            ShuffleQuestions = shuffleQuestions,
            ShuffleAnswers = shuffleAnswers,
            IsMockExam = isMockExam,
            IsPublished = true
        };

        test.Questions = questions.Select((q, index) => new PracticeTestQuestion
        {
            Id = Guid.NewGuid(),
            PracticeTestId = test.Id,
            QuestionBankQuestionId = q.Id,
            Text = q.Text,
            OptionA = q.OptionA,
            OptionB = q.OptionB,
            OptionC = q.OptionC,
            OptionD = q.OptionD,
            OptionE = q.OptionE,
            OptionF = q.OptionF,
            CorrectOption = q.CorrectOption,
            Explanation = q.Explanation,
            Order = index + 1
        }).ToList();

        return test;
    }

    private QuestionBankQuestion? BuildQuestionFromRow(Dictionary<string, string> row, List<Category> categories, List<string> errors, int rowNumber)
    {
        var categoryValue = Get(row, "Category");
        var category = categories.FirstOrDefault(c => c.Name.Equals(categoryValue, StringComparison.OrdinalIgnoreCase) || c.Slug.Equals(categoryValue, StringComparison.OrdinalIgnoreCase));
        if (category == null) errors.Add($"Row {rowNumber}: Category '{categoryValue}' was not found.");

        var difficulty = NormalizeChoice(Get(row, "Difficulty"), Difficulties, "Easy");
        var status = NormalizeChoice(Get(row, "Status"), Statuses, "Draft");
        var correct = NormalizeCorrect(Get(row, "CorrectAnswer"));
        var estimated = int.TryParse(Get(row, "EstimatedTime"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) ? seconds : 60;

        var question = new QuestionBankQuestion
        {
            Id = Guid.NewGuid(),
            CategoryId = category?.Id ?? 0,
            Topic = Get(row, "Topic"),
            Subtopic = Get(row, "Subtopic"),
            Difficulty = difficulty,
            Text = Get(row, "QuestionText"),
            QuestionImageUrl = EmptyToNull(Get(row, "QuestionImage")),
            ExplanationImageUrl = EmptyToNull(Get(row, "ExplanationImage")),
            OptionA = Get(row, "OptionA"),
            OptionB = Get(row, "OptionB"),
            OptionC = Get(row, "OptionC"),
            OptionD = Get(row, "OptionD"),
            OptionE = EmptyToNull(Get(row, "OptionE")),
            OptionF = EmptyToNull(Get(row, "OptionF")),
            CorrectOption = correct,
            Explanation = Get(row, "DetailedExplanation"),
            WrongAnswerExplanation = Get(row, "WrongAnswerExplanation"),
            SourceReference = Get(row, "SourceReference"),
            Tags = Get(row, "Tags"),
            EstimatedTimeSeconds = Math.Max(15, estimated),
            Status = status,
            CreatedBy = CurrentUserName(),
            ModifiedBy = CurrentUserName()
        };

        var rowErrors = ValidateQuestionValues(question).Select(e => $"Row {rowNumber}: {e}").ToList();
        errors.AddRange(rowErrors);
        return category != null && !rowErrors.Any() ? question : null;
    }

    private void Normalize(QuestionBankQuestion question, bool isNew)
    {
        question.Topic = (question.Topic ?? string.Empty).Trim();
        question.Subtopic = (question.Subtopic ?? string.Empty).Trim();
        question.Difficulty = NormalizeChoice(question.Difficulty, Difficulties, "Easy");
        question.Status = NormalizeChoice(question.Status, Statuses, "Draft");
        question.Text = (question.Text ?? string.Empty).Trim();
        question.OptionA = (question.OptionA ?? string.Empty).Trim();
        question.OptionB = (question.OptionB ?? string.Empty).Trim();
        question.OptionC = (question.OptionC ?? string.Empty).Trim();
        question.OptionD = (question.OptionD ?? string.Empty).Trim();
        question.OptionE = EmptyToNull(question.OptionE);
        question.OptionF = EmptyToNull(question.OptionF);
        question.CorrectOption = NormalizeCorrect(question.CorrectOption);
        question.Explanation = (question.Explanation ?? string.Empty).Trim();
        question.WrongAnswerExplanation = (question.WrongAnswerExplanation ?? string.Empty).Trim();
        question.SourceReference = (question.SourceReference ?? string.Empty).Trim();
        question.Tags = (question.Tags ?? string.Empty).Trim();
        question.EstimatedTimeSeconds = Math.Max(15, question.EstimatedTimeSeconds);
        if (isNew) question.CreatedAt = DateTime.UtcNow;
        question.UpdatedAt = DateTime.UtcNow;
    }

    private void ValidateQuestion(QuestionBankQuestion question)
    {
        foreach (var error in ValidateQuestionValues(question)) ModelState.AddModelError(string.Empty, error);
        ModelState.Remove(nameof(question.Category));
        ModelState.Remove(nameof(question.PracticeTestQuestions));
    }

    private static IEnumerable<string> ValidateQuestionValues(QuestionBankQuestion question)
    {
        if (question.CategoryId <= 0) yield return "Category is required.";
        if (string.IsNullOrWhiteSpace(question.Topic)) yield return "Topic is required.";
        if (string.IsNullOrWhiteSpace(question.Text)) yield return "Question text is required.";
        if (string.IsNullOrWhiteSpace(question.OptionA) || string.IsNullOrWhiteSpace(question.OptionB) || string.IsNullOrWhiteSpace(question.OptionC) || string.IsNullOrWhiteSpace(question.OptionD)) yield return "Options A-D are required.";
        if (string.IsNullOrWhiteSpace(question.Explanation)) yield return "Detailed explanation is required.";
        var choices = new Dictionary<string, string?> { ["A"] = question.OptionA, ["B"] = question.OptionB, ["C"] = question.OptionC, ["D"] = question.OptionD, ["E"] = question.OptionE, ["F"] = question.OptionF };
        if (!choices.TryGetValue(question.CorrectOption, out var answer) || string.IsNullOrWhiteSpace(answer)) yield return "Correct answer must match a populated answer choice.";
    }

    private async Task LoadLookups()
    {
        ViewBag.Categories = await Categories();
        ViewBag.Difficulties = Difficulties;
        ViewBag.Statuses = Statuses;
    }

    private Task<List<Category>> Categories() => _db.Categories.Where(c => c.IsActive).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync();

    private string CurrentUserName() => User.Identity?.Name ?? User.FindFirstValue(ClaimTypes.Email) ?? "Admin";

    private static string NormalizeChoice(string? value, string[] allowed, string fallback) => allowed.FirstOrDefault(x => x.Equals(value?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? fallback;
    private static string NormalizeCorrect(string? value) => string.IsNullOrWhiteSpace(value) ? "A" : value.Trim()[0].ToString().ToUpperInvariant();
    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Get(Dictionary<string, string> row, string key) => row.TryGetValue(key, out var value) ? value.Trim() : string.Empty;

    private static List<Dictionary<string, string>> ReadCsvRows(IFormFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        var header = ParseCsvLine(reader.ReadLine() ?? string.Empty);
        var rows = new List<Dictionary<string, string>>();
        while (!reader.EndOfStream)
        {
            var values = ParseCsvLine(reader.ReadLine() ?? string.Empty);
            if (values.All(string.IsNullOrWhiteSpace)) continue;
            rows.Add(header.Select((h, i) => new { h, v = i < values.Count ? values[i] : string.Empty }).ToDictionary(x => x.h, x => x.v, StringComparer.OrdinalIgnoreCase));
        }
        return rows;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"' && quoted && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
            else if (ch == '"') quoted = !quoted;
            else if (ch == ',' && !quoted) { values.Add(current.ToString()); current.Clear(); }
            else current.Append(ch);
        }
        values.Add(current.ToString());
        return values;
    }

    private static List<Dictionary<string, string>> ReadXlsxRows(IFormFile file)
    {
        using var archive = new ZipArchive(file.OpenReadStream(), ZipArchiveMode.Read);
        var sharedStrings = ReadSharedStrings(archive);
        var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml");
        if (sheetEntry == null) return new List<Dictionary<string, string>>();
        using var stream = sheetEntry.Open();
        var doc = XDocument.Load(stream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var rows = doc.Descendants(ns + "row").Select(row => row.Elements(ns + "c").Select(cell => ReadCell(cell, sharedStrings, ns)).ToList()).Where(r => r.Any(v => !string.IsNullOrWhiteSpace(v))).ToList();
        if (!rows.Any()) return new List<Dictionary<string, string>>();
        var header = rows[0];
        return rows.Skip(1).Select(values => header.Select((h, i) => new { h, v = i < values.Count ? values[i] : string.Empty }).ToDictionary(x => x.h, x => x.v, StringComparer.OrdinalIgnoreCase)).ToList();
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");
        if (entry == null) return new List<string>();
        using var stream = entry.Open();
        var doc = XDocument.Load(stream);
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return doc.Descendants(ns + "si").Select(si => string.Concat(si.Descendants(ns + "t").Select(t => t.Value))).ToList();
    }

    private static string ReadCell(XElement cell, List<string> sharedStrings, XNamespace ns)
    {
        var cellType = (string?)cell.Attribute("t");
        if (cellType == "inlineStr") return cell.Element(ns + "is")?.Element(ns + "t")?.Value ?? string.Empty;
        var value = cell.Element(ns + "v")?.Value ?? string.Empty;
        if (cellType == "s" && int.TryParse(value, out var index) && index >= 0 && index < sharedStrings.Count) return sharedStrings[index];
        return value;
    }

    private static List<string[]> ExportRows(List<QuestionBankQuestion> questions)
    {
        var rows = new List<string[]>
        {
            new[] { "Category", "Topic", "Subtopic", "Difficulty", "QuestionText", "QuestionImage", "OptionA", "OptionB", "OptionC", "OptionD", "OptionE", "OptionF", "CorrectAnswer", "DetailedExplanation", "ExplanationImage", "WrongAnswerExplanation", "SourceReference", "Tags", "EstimatedTime", "Status" }
        };
        rows.AddRange(questions.Select(q => new[] { q.Category.Name, q.Topic, q.Subtopic, q.Difficulty, q.Text, q.QuestionImageUrl ?? string.Empty, q.OptionA, q.OptionB, q.OptionC, q.OptionD, q.OptionE ?? string.Empty, q.OptionF ?? string.Empty, q.CorrectOption, q.Explanation, q.ExplanationImageUrl ?? string.Empty, q.WrongAnswerExplanation, q.SourceReference, q.Tags, q.EstimatedTimeSeconds.ToString(CultureInfo.InvariantCulture), q.Status }));
        return rows;
    }

    private static string BuildCsv(List<string[]> rows) => string.Join(Environment.NewLine, rows.Select(row => string.Join(",", row.Select(EscapeCsv))));
    private static string EscapeCsv(string value) => value.Contains(',') || value.Contains('"') || value.Contains('\n') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;

    private static byte[] BuildXlsx(List<string[]> rows)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            AddEntry(archive, "[Content_Types].xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/></Types>");
            AddEntry(archive, "_rels/.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/></Relationships>");
            AddEntry(archive, "xl/workbook.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets><sheet name=\"Question Bank\" sheetId=\"1\" r:id=\"rId1\"/></sheets></workbook>");
            AddEntry(archive, "xl/_rels/workbook.xml.rels", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\"><Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet1.xml\"/></Relationships>");
            var sheet = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
            for (var r = 0; r < rows.Count; r++)
            {
                sheet.Append(CultureInfo.InvariantCulture, $"<row r=\"{r + 1}\">");
                for (var c = 0; c < rows[r].Length; c++) sheet.Append(CultureInfo.InvariantCulture, $"<c r=\"{ColumnName(c)}{r + 1}\" t=\"inlineStr\"><is><t>{System.Security.SecurityElement.Escape(rows[r][c])}</t></is></c>");
                sheet.Append("</row>");
            }
            sheet.Append("</sheetData></worksheet>");
            AddEntry(archive, "xl/worksheets/sheet1.xml", sheet.ToString());
        }
        return ms.ToArray();
    }

    private static void AddEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }

    private static string ColumnName(int index)
    {
        var name = string.Empty;
        index++;
        while (index > 0)
        {
            var modulo = (index - 1) % 26;
            name = Convert.ToChar('A' + modulo) + name;
            index = (index - modulo) / 26;
        }
        return name;
    }
}



