using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SharedLayer.Enums;

namespace UserLayer.Controllers;

public class CoursesController : Controller
{
    private readonly ICourseService _courseService;
    private readonly ICategoryService _categoryService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IReviewService _reviewService;
    private readonly ILessonService _lessonService;
    private readonly IProgressService _progressService;

    public CoursesController(ICourseService courseService, ICategoryService categoryService,
        IEnrollmentService enrollmentService, IReviewService reviewService,
        ILessonService lessonService, IProgressService progressService)
    {
        _courseService = courseService;
        _categoryService = categoryService;
        _enrollmentService = enrollmentService;
        _reviewService = reviewService;
        _lessonService = lessonService;
        _progressService = progressService;
    }

    public async Task<IActionResult> Index(string? q, int? categoryId, string? level, string? sort)
    {
        ViewData["Title"] = string.IsNullOrEmpty(q) ? "All Courses" : $"Results for \"{q}\"";
        ViewData["SearchQuery"] = q;

        var coursesResult = await _courseService.SearchCoursesAsync(q ?? "");
        var categoriesResult = await _categoryService.GetAllCategoriesAsync();

        var courses = coursesResult.Data ?? Enumerable.Empty<DataLayer.Entities.Course>();

        if (categoryId.HasValue)
            courses = courses.Where(c => c.CategoryId == categoryId.Value);
        if (!string.IsNullOrEmpty(level) && Enum.TryParse<CourseLevel>(level, out var lvl))
            courses = courses.Where(c => c.Level == lvl);

        courses = sort switch
        {
            "popular"    => courses.OrderByDescending(c => c.TotalEnrollments),
            "rating"     => courses.OrderByDescending(c => c.AverageRating),
            "newest"     => courses.OrderByDescending(c => c.PublishedAt),
            "price_low"  => courses.OrderBy(c => c.DiscountedPrice ?? c.Price),
            "price_high" => courses.OrderByDescending(c => c.DiscountedPrice ?? c.Price),
            _            => courses.OrderByDescending(c => c.IsFeatured)
        };

        ViewBag.Courses = courses.ToList();
        ViewBag.Categories = (categoriesResult.Data ?? Enumerable.Empty<DataLayer.Entities.Category>()).ToList();
        ViewBag.SelectedCategory = categoryId;
        ViewBag.SelectedLevel = level;
        ViewBag.SelectedSort = sort;
        ViewBag.TotalCount = courses.Count();

        return View();
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var courseResult = await _courseService.GetCourseByIdAsync(id);
        if (!courseResult.IsSuccess || courseResult.Data == null) return NotFound();

        var course = courseResult.Data;
        ViewData["Title"] = course.Title;

        var lessons = course.Sections
            .SelectMany(s => s.Lessons)
            .Where(l => l.IsPublished)
            .OrderBy(l => l.Section.Order)
            .ThenBy(l => l.Order)
            .ToList();

        bool isEnrolled = false;
        DataLayer.Entities.Enrollment? enrollment = null;
        IReadOnlySet<Guid> completedLessons = new HashSet<Guid>();
        if (User.Identity?.IsAuthenticated == true)
        {
            if (!TryGetCurrentUserId(out var studentId))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(Details), new { id }) });
            }
            var enrollmentResult = await _enrollmentService.GetEnrollmentAsync(studentId, id);
            if (enrollmentResult.IsSuccess && enrollmentResult.Data != null && enrollmentResult.Data.Status is EnrollmentStatus.Active or EnrollmentStatus.Completed)
            {
                enrollment = enrollmentResult.Data;
                isEnrolled = true;
                var completedResult = await _progressService.GetCompletedLessonIdsAsync(enrollment.Id);
                completedLessons = completedResult.Data ?? new HashSet<Guid>();
            }
        }

        ViewBag.IsEnrolled = isEnrolled;
        ViewBag.Enrollment = enrollment;
        ViewBag.Lessons = lessons;
        ViewBag.CompletedLessonIds = completedLessons;
        ViewBag.FirstLesson = lessons.FirstOrDefault();
        ViewBag.Course = course;
        return View(course);
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(Guid courseId)
    {
        if (!TryGetCurrentUserId(out var studentId))
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Error"] = "Please sign in again before enrolling.";
            return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(Details), new { id = courseId }) });
        }

        var result = await _enrollmentService.EnrollStudentAsync(studentId, courseId);

        if (result.IsSuccess)
        {
            TempData["Success"] = "You have successfully enrolled in this course!";
            var lessonsResult = await _lessonService.GetLessonsByCourseAsync(courseId);
            var firstLesson = (lessonsResult.Data ?? Enumerable.Empty<DataLayer.Entities.Lesson>())
                .Where(l => l.IsPublished)
                .OrderBy(l => l.Section.Order)
                .ThenBy(l => l.Order)
                .FirstOrDefault();
            if (firstLesson != null) return RedirectToAction("Watch", "Lessons", new { id = firstLesson.Id });
        }
        else
        {
            TempData["Error"] = result.ErrorMessage;
            if (string.Equals(result.ErrorMessage, "Your sign-in session is no longer valid. Please sign in again.", StringComparison.Ordinal))
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(Details), new { id = courseId }) });
            }
        }

        return RedirectToAction(nameof(Details), new { id = courseId });
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var value = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out userId);
    }
}


