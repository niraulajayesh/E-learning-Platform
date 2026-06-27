using BusinessLayer.Interfaces;
using DataLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminLayer.Controllers;

public class LessonsController : Controller
{
    private const long MaxVideoBytes = 200 * 1024 * 1024;
    private const long MaxPdfBytes = 25 * 1024 * 1024;
    private static readonly HashSet<string> AllowedVideoTypes = new(StringComparer.OrdinalIgnoreCase) { "video/mp4", "video/webm", "video/quicktime" };
    private static readonly HashSet<string> AllowedPdfTypes = new(StringComparer.OrdinalIgnoreCase) { "application/pdf" };

    private readonly ILessonService _lessonService;
    private readonly ICourseService _courseService;
    private readonly IWebHostEnvironment _environment;

    public LessonsController(ILessonService lessonService, ICourseService courseService, IWebHostEnvironment environment)
    {
        _lessonService = lessonService;
        _courseService = courseService;
        _environment = environment;
    }

    [HttpGet("Lessons")]
    public async Task<IActionResult> All(string? search, int page = 1)
    {
        const int pageSize = 10;
        var coursesResult = await _courseService.GetAllCoursesAsync();
        var courses = coursesResult.Data ?? Enumerable.Empty<Course>();
        if (!string.IsNullOrWhiteSpace(search)) courses = courses.Where(c => c.Title.Contains(search, StringComparison.OrdinalIgnoreCase));
        var total = courses.Count();
        ViewBag.Search = search;
        var currentPage = Math.Max(1, page);
        ViewBag.Page = currentPage;
        ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        ViewData["Title"] = "Lessons";
        return View(courses.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList());
    }
    [HttpGet("Lessons/Index/{courseId:guid}")]
    public async Task<IActionResult> Index(Guid courseId, string? search)
    {
        var courseResult = await _courseService.GetCourseByIdAsync(courseId);
        if (!courseResult.IsSuccess || courseResult.Data == null) return NotFound();

        var result = await _lessonService.GetLessonsByCourseAsync(courseId);
        var lessons = result.Data ?? Enumerable.Empty<Lesson>();
        if (!string.IsNullOrWhiteSpace(search)) lessons = lessons.Where(l => l.Title.Contains(search, StringComparison.OrdinalIgnoreCase));
        ViewBag.CourseId = courseId;
        ViewBag.CourseTitle = courseResult.Data.Title;
        ViewBag.Search = search;
        return View(lessons.ToList());
    }

    [HttpGet("Lessons/Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        var result = await _lessonService.GetLessonByIdAsync(id);
        if (!result.IsSuccess || result.Data == null) return NotFound();
        return View(result.Data);
    }

    [HttpGet("Lessons/Preview/{id:guid}")]
    public async Task<IActionResult> Preview(Guid id)
    {
        var result = await _lessonService.GetLessonByIdAsync(id);
        if (!result.IsSuccess || result.Data == null) return NotFound();
        return View(result.Data);
    }

    [HttpGet("Lessons/Create/{courseId:guid}")]
    public async Task<IActionResult> Create(Guid courseId)
    {
        var courseResult = await _courseService.GetCourseByIdAsync(courseId);
        if (!courseResult.IsSuccess || courseResult.Data == null) return NotFound();

        ViewBag.CourseId = courseId;
        ViewBag.CourseTitle = courseResult.Data.Title;
        return View(new Lesson { IsPublished = true, Type = SharedLayer.Enums.LessonType.Video });
    }

    [HttpPost("Lessons/Create/{courseId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Guid courseId, Lesson lesson, IFormFile? videoFile, IFormFile? pdfFile)
    {
        ClearLessonModelState();
        ValidateLesson(lesson);
        await ApplyUploadsAsync(lesson, videoFile, pdfFile);

        if (!ModelState.IsValid)
        {
            await PopulateCourseViewBag(courseId);
            return View(lesson);
        }

        var result = await _lessonService.CreateLessonForCourseAsync(courseId, lesson);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Lesson created.";
            return RedirectToAction(nameof(Index), new { courseId });
        }

        await PopulateCourseViewBag(courseId);
        ModelState.AddModelError("", result.ErrorMessage!);
        return View(lesson);
    }

    [HttpGet("Lessons/Edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await _lessonService.GetLessonByIdAsync(id);
        if (!result.IsSuccess || result.Data == null) return NotFound();
        await PopulateCourseViewBag(result.Data.Section.CourseId);
        return View(result.Data);
    }

    [HttpPost("Lessons/Edit/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Lesson model, IFormFile? videoFile, IFormFile? pdfFile, bool deletePdf = false)
    {
        ClearLessonModelState();
        ValidateLesson(model);
        var existing = await _lessonService.GetLessonByIdAsync(id);
        if (!existing.IsSuccess || existing.Data == null) return NotFound();

        var lesson = existing.Data;
        var oldVideo = lesson.ContentUrl;
        var oldPdf = lesson.ResourcesUrl;
        lesson.Title = model.Title;
        lesson.Description = model.Description;
        lesson.Type = model.Type;
        lesson.ContentUrl = model.ContentUrl;
        lesson.ArticleContent = model.ArticleContent;
        lesson.ResourcesUrl = model.ResourcesUrl;
        lesson.DurationMinutes = model.DurationMinutes;
        lesson.IsFreePreview = model.IsFreePreview;
        lesson.IsPublished = model.IsPublished;

        await ApplyUploadsAsync(lesson, videoFile, pdfFile);
        if (deletePdf && pdfFile == null) lesson.ResourcesUrl = null;

        if (!ModelState.IsValid)
        {
            await PopulateCourseViewBag(lesson.Section.CourseId);
            return View(lesson);
        }

        var result = await _lessonService.UpdateLessonAsync(lesson);
        if (result.IsSuccess)
        {
            if (!string.Equals(oldVideo, lesson.ContentUrl, StringComparison.OrdinalIgnoreCase)) DeleteLocalFile(oldVideo);
            if (deletePdf || !string.Equals(oldPdf, lesson.ResourcesUrl, StringComparison.OrdinalIgnoreCase)) DeleteLocalFile(oldPdf);
            TempData["Success"] = "Lesson updated.";
            return RedirectToAction(nameof(Index), new { courseId = lesson.Section.CourseId });
        }

        await PopulateCourseViewBag(lesson.Section.CourseId);
        ModelState.AddModelError("", result.ErrorMessage!);
        return View(lesson);
    }

    [HttpPost("Lessons/Delete/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var lessonResult = await _lessonService.GetLessonByIdAsync(id);
        if (!lessonResult.IsSuccess || lessonResult.Data == null) return NotFound();
        var courseId = lessonResult.Data.Section.CourseId;
        var result = await _lessonService.DeleteLessonAsync(id);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Lesson deleted." : result.ErrorMessage;
        return RedirectToAction(nameof(Index), new { courseId });
    }

    [HttpPost("Lessons/Reorder/{courseId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(Guid courseId, List<Guid> lessonIds)
    {
        var result = await _lessonService.ReorderLessonsAsync(courseId, lessonIds);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Lesson order saved." : result.ErrorMessage;
        return RedirectToAction(nameof(Index), new { courseId });
    }

    private async Task PopulateCourseViewBag(Guid courseId)
    {
        var courseResult = await _courseService.GetCourseByIdAsync(courseId);
        ViewBag.CourseId = courseId;
        ViewBag.CourseTitle = courseResult.Data?.Title ?? "Course";
    }

    private void ValidateLesson(Lesson lesson)
    {
        if (string.IsNullOrWhiteSpace(lesson.Title)) ModelState.AddModelError(nameof(Lesson.Title), "Title is required.");
        if (lesson.DurationMinutes < 0) ModelState.AddModelError(nameof(Lesson.DurationMinutes), "Duration cannot be negative.");
    }

    private void ClearLessonModelState()
    {
        ModelState.Remove(nameof(Lesson.Section));
        ModelState.Remove(nameof(Lesson.Quiz));
        ModelState.Remove(nameof(Lesson.ProgressRecords));
    }

    private async Task ApplyUploadsAsync(Lesson lesson, IFormFile? videoFile, IFormFile? pdfFile)
    {
        var video = await SaveUploadAsync(videoFile, "uploads/videos", AllowedVideoTypes, MaxVideoBytes, [".mp4", ".webm", ".mov"]);
        if (!video.Success) ModelState.AddModelError(nameof(Lesson.ContentUrl), video.Error!);
        if (video.Path != null) lesson.ContentUrl = video.Path;

        var pdf = await SaveUploadAsync(pdfFile, "uploads/resources", AllowedPdfTypes, MaxPdfBytes, [".pdf"]);
        if (!pdf.Success) ModelState.AddModelError(nameof(Lesson.ResourcesUrl), pdf.Error!);
        if (pdf.Path != null) lesson.ResourcesUrl = pdf.Path;
    }

    private async Task<(bool Success, string? Path, string? Error)> SaveUploadAsync(IFormFile? file, string folder, HashSet<string> allowedContentTypes, long maxBytes, string[] allowedExtensions)
    {
        if (file == null || file.Length == 0) return (true, null, null);
        if (file.Length > maxBytes) return (false, null, $"File is too large. Maximum size is {maxBytes / 1024 / 1024} MB.");
        if (!allowedContentTypes.Contains(file.ContentType)) return (false, null, "Invalid file type.");
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)) return (false, null, "Invalid file extension.");

        var safeName = $"{Guid.NewGuid():N}{extension}";
        var relativeFolder = folder.Replace('/', Path.DirectorySeparatorChar);
        var absoluteFolder = Path.Combine(_environment.WebRootPath, relativeFolder);
        Directory.CreateDirectory(absoluteFolder);
        var absolutePath = Path.Combine(absoluteFolder, safeName);
        await using var stream = System.IO.File.Create(absolutePath);
        await file.CopyToAsync(stream);
        return (true, "/" + folder.Trim('/').Replace("\\", "/") + "/" + safeName, null);
    }

    private void DeleteLocalFile(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || !relativePath.StartsWith('/')) return;
        var absolutePath = Path.GetFullPath(Path.Combine(_environment.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        var webRoot = Path.GetFullPath(_environment.WebRootPath);
        if (absolutePath.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase) && System.IO.File.Exists(absolutePath)) System.IO.File.Delete(absolutePath);
    }
}






