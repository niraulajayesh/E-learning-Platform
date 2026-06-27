using BusinessLayer.Interfaces;
using DataLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedLayer.Enums;

namespace AdminLayer.Controllers;

public class CoursesController : Controller
{
    private const int PageSize = 10;
    private const long MaxThumbnailBytes = 2 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp", "image/gif" };

    private readonly ICourseService _courseService;
    private readonly ICategoryService _categoryService;
    private readonly IUserService _userService;
    private readonly IWebHostEnvironment _environment;

    public CoursesController(ICourseService courseService, ICategoryService categoryService, IUserService userService, IWebHostEnvironment environment)
    {
        _courseService = courseService;
        _categoryService = categoryService;
        _userService = userService;
        _environment = environment;
    }

    public async Task<IActionResult> Index(string? search, int? categoryId, Guid? instructorId, CourseStatus? status, int page = 1)
    {
        var result = await _courseService.GetAllCoursesAsync();
        var courses = result.Data ?? Enumerable.Empty<Course>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            courses = courses.Where(c => c.Title.Contains(search, StringComparison.OrdinalIgnoreCase) || c.Slug.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        if (categoryId.HasValue) courses = courses.Where(c => c.CategoryId == categoryId.Value);
        if (status.HasValue) courses = courses.Where(c => c.Status == status.Value);
        if (instructorId.HasValue) courses = courses.Where(c => c.InstructorId == instructorId.Value);

        var total = courses.Count();
        page = Math.Max(1, page);
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.Status = status;
        ViewBag.InstructorId = instructorId;
        ViewBag.Page = page;
        ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        ViewBag.TotalItems = total;
        ViewBag.Categories = ((await _categoryService.GetAllCategoriesAsync()).Data ?? Enumerable.Empty<Category>()).Where(c => c.IsActive).ToList();
        ViewBag.Instructors = ((await _userService.GetAllUsersAsync()).Data ?? Enumerable.Empty<User>()).Where(u => u.Role == UserRole.Instructor || u.Role == UserRole.Admin).OrderBy(u => u.FirstName).ToList();

        return View(courses.Skip((page - 1) * PageSize).Take(PageSize).ToList());
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var result = await _courseService.GetCourseByIdAsync(id);
        if (!result.IsSuccess || result.Data == null) return NotFound();
        return View(result.Data);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateLookups();
        return View(new Course { Status = CourseStatus.Draft, Language = "English", Level = CourseLevel.AllLevels });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Course course, IFormFile? thumbnailFile)
    {
        ClearCourseModelState();
        ValidateCourse(course);
        var uploadResult = await SaveUploadAsync(thumbnailFile, "uploads/course-thumbnails", AllowedImageTypes, MaxThumbnailBytes);
        if (!uploadResult.Success) ModelState.AddModelError(nameof(Course.ThumbnailUrl), uploadResult.Error!);
        if (uploadResult.Path != null) course.ThumbnailUrl = uploadResult.Path;

        if (!ModelState.IsValid)
        {
            await PopulateLookups();
            return View(course);
        }

        var result = await _courseService.CreateCourseAsync(course);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Course created.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateLookups();
        ModelState.AddModelError("", result.ErrorMessage!);
        return View(course);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await _courseService.GetCourseByIdAsync(id);
        if (!result.IsSuccess || result.Data == null) return NotFound();
        await PopulateLookups();
        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Course model, IFormFile? thumbnailFile)
    {
        ClearCourseModelState();
        ValidateCourse(model);

        var existing = await _courseService.GetCourseByIdAsync(model.Id);
        if (!existing.IsSuccess || existing.Data == null) return NotFound();

        var uploadResult = await SaveUploadAsync(thumbnailFile, "uploads/course-thumbnails", AllowedImageTypes, MaxThumbnailBytes);
        if (!uploadResult.Success) ModelState.AddModelError(nameof(Course.ThumbnailUrl), uploadResult.Error!);

        if (!ModelState.IsValid)
        {
            await PopulateLookups();
            return View(model);
        }

        var course = existing.Data;
        var oldThumbnail = course.ThumbnailUrl;
        course.InstructorId = model.InstructorId;
        course.CategoryId = model.CategoryId;
        course.Title = model.Title;
        course.Slug = model.Slug;
        course.ShortDescription = model.ShortDescription;
        course.Description = model.Description;
        course.ThumbnailUrl = uploadResult.Path ?? model.ThumbnailUrl;
        course.PreviewVideoUrl = model.PreviewVideoUrl;
        course.Price = model.Price;
        course.DiscountedPrice = model.DiscountedPrice;
        course.Level = model.Level;
        course.Status = model.Status;
        course.Language = model.Language;
        course.WhatYouWillLearn = model.WhatYouWillLearn;
        course.Requirements = model.Requirements;
        course.TargetAudience = model.TargetAudience;
        course.IsFeatured = model.IsFeatured;
        course.IsBestseller = model.IsBestseller;

        var result = await _courseService.UpdateCourseAsync(course);
        if (result.IsSuccess)
        {
            if (uploadResult.Path != null && !string.Equals(oldThumbnail, uploadResult.Path, StringComparison.OrdinalIgnoreCase)) DeleteLocalFile(oldThumbnail);
            TempData["Success"] = "Course updated.";
            return RedirectToAction(nameof(Index));
        }

        await PopulateLookups();
        ModelState.AddModelError("", result.ErrorMessage!);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _courseService.DeleteCourseAsync(id);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Course archived safely." : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(Guid id)
    {
        var result = await _courseService.PublishCourseAsync(id);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Course published." : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unpublish(Guid id)
    {
        var result = await _courseService.UnpublishCourseAsync(id);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Course unpublished." : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleFeatured(Guid id)
    {
        var result = await _courseService.ToggleFeaturedAsync(id);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Featured status updated." : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateLookups()
    {
        var categories = (await _categoryService.GetAllCategoriesAsync()).Data ?? Enumerable.Empty<Category>();
        var users = (await _userService.GetAllUsersAsync()).Data ?? Enumerable.Empty<User>();
        ViewBag.Categories = categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToList();
        ViewBag.Instructors = users.Where(u => u.Role == UserRole.Instructor || u.Role == UserRole.Admin).OrderBy(u => u.FirstName).ToList();
    }

    private void ValidateCourse(Course course)
    {
        if (string.IsNullOrWhiteSpace(course.Title)) ModelState.AddModelError(nameof(Course.Title), "Title is required.");
        if (string.IsNullOrWhiteSpace(course.ShortDescription)) ModelState.AddModelError(nameof(Course.ShortDescription), "Short description is required.");
        if (string.IsNullOrWhiteSpace(course.Description)) ModelState.AddModelError(nameof(Course.Description), "Description is required.");
        if (course.Price < 0) ModelState.AddModelError(nameof(Course.Price), "Price cannot be negative.");
        if (course.DiscountedPrice.HasValue && course.DiscountedPrice.Value < 0) ModelState.AddModelError(nameof(Course.DiscountedPrice), "Discounted price cannot be negative.");
        if (course.DiscountedPrice.HasValue && course.DiscountedPrice.Value > course.Price) ModelState.AddModelError(nameof(Course.DiscountedPrice), "Discounted price cannot exceed the price.");
    }

    private void ClearCourseModelState()
    {
        ModelState.Remove(nameof(Course.Instructor));
        ModelState.Remove(nameof(Course.Category));
        ModelState.Remove(nameof(Course.Sections));
        ModelState.Remove(nameof(Course.Enrollments));
        ModelState.Remove(nameof(Course.Reviews));
        ModelState.Remove(nameof(Course.Payments));
        ModelState.Remove(nameof(Course.Certificates));
    }

    private async Task<(bool Success, string? Path, string? Error)> SaveUploadAsync(IFormFile? file, string folder, HashSet<string> allowedContentTypes, long maxBytes)
    {
        if (file == null || file.Length == 0) return (true, null, null);
        if (file.Length > maxBytes) return (false, null, $"File is too large. Maximum size is {maxBytes / 1024 / 1024} MB.");
        if (!allowedContentTypes.Contains(file.ContentType)) return (false, null, "Invalid file type.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
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


