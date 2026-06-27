using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedLayer.Enums;

namespace UserLayer.Controllers;

[Authorize]
public class MyCoursesController : Controller
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly ILessonService _lessonService;
    private readonly IProgressService _progressService;

    public MyCoursesController(IEnrollmentService enrollmentService, ILessonService lessonService, IProgressService progressService)
    {
        _enrollmentService = enrollmentService;
        _lessonService = lessonService;
        _progressService = progressService;
    }

    public async Task<IActionResult> Index(string? filter)
    {
        var studentId = Guid.Parse(User.FindFirst("UserId")!.Value);
        ViewData["Title"] = "My Courses";
        ViewBag.Filter = filter;

        var result = await _enrollmentService.GetStudentEnrollmentsAsync(studentId);
        var enrollments = result.Data ?? Enumerable.Empty<DataLayer.Entities.Enrollment>();

        enrollments = filter switch
        {
            "active"    => enrollments.Where(e => e.Status == EnrollmentStatus.Active),
            "completed" => enrollments.Where(e => e.Status == EnrollmentStatus.Completed),
            _           => enrollments
        };

        var list = enrollments.ToList();
        var nextLessonByCourse = new Dictionary<Guid, Guid>();
        foreach (var enrollment in list)
        {
            var lessonsResult = await _lessonService.GetLessonsByCourseAsync(enrollment.CourseId);
            var lessons = (lessonsResult.Data ?? Enumerable.Empty<DataLayer.Entities.Lesson>())
                .Where(l => l.IsPublished)
                .OrderBy(l => l.Section.Order)
                .ThenBy(l => l.Order)
                .ToList();

            var completedResult = await _progressService.GetCompletedLessonIdsAsync(enrollment.Id);
            var completedLessonIds = completedResult.Data ?? new HashSet<Guid>();
            var nextLesson = lessons.FirstOrDefault(l => !completedLessonIds.Contains(l.Id)) ?? lessons.FirstOrDefault();
            if (nextLesson != null) nextLessonByCourse[enrollment.CourseId] = nextLesson.Id;
        }

        ViewBag.NextLessonByCourse = nextLessonByCourse;
        return View(list);
    }
}
