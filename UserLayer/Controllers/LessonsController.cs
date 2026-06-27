using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedLayer.Enums;
using UserLayer.Models;

namespace UserLayer.Controllers;

[Authorize]
public class LessonsController : Controller
{
    private readonly ILessonService _lessonService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IProgressService _progressService;

    public LessonsController(ILessonService lessonService, IEnrollmentService enrollmentService, IProgressService progressService)
    {
        _lessonService = lessonService;
        _enrollmentService = enrollmentService;
        _progressService = progressService;
    }

    public async Task<IActionResult> Watch(Guid id)
    {
        var lessonResult = await _lessonService.GetLessonByIdAsync(id);
        if (!lessonResult.IsSuccess || lessonResult.Data == null) return NotFound();

        var lesson = lessonResult.Data;
        if (!lesson.IsPublished) return NotFound();

        var course = lesson.Section.Course;
        var studentId = Guid.Parse(User.FindFirst("UserId")!.Value);
        var enrollmentResult = await _enrollmentService.GetEnrollmentAsync(studentId, course.Id);
        if (!enrollmentResult.IsSuccess || enrollmentResult.Data == null)
        {
            TempData["Error"] = "Enroll in this course to access lessons.";
            return RedirectToAction("Details", "Courses", new { id = course.Id });
        }

        var enrollment = enrollmentResult.Data;
        if (enrollment.Status is not (EnrollmentStatus.Active or EnrollmentStatus.Completed))
        {
            TempData["Error"] = "Your enrollment is not active for this course.";
            return RedirectToAction("Details", "Courses", new { id = course.Id });
        }

        await _progressService.UpdateProgressAsync(enrollment.Id, lesson.Id, 0, false);
        enrollmentResult = await _enrollmentService.GetEnrollmentAsync(studentId, course.Id);
        enrollment = enrollmentResult.Data ?? enrollment;

        var lessonsResult = await _lessonService.GetLessonsByCourseAsync(course.Id);
        var lessons = (lessonsResult.Data ?? Enumerable.Empty<DataLayer.Entities.Lesson>())
            .Where(l => l.IsPublished)
            .OrderBy(l => l.Section.Order)
            .ThenBy(l => l.Order)
            .ToList();

        var completedResult = await _progressService.GetCompletedLessonIdsAsync(enrollment.Id);
        var completedLessonIds = completedResult.Data ?? new HashSet<Guid>();
        var index = lessons.FindIndex(l => l.Id == lesson.Id);

        ViewData["Title"] = lesson.Title;
        return View(new LessonWatchViewModel
        {
            Enrollment = enrollment,
            Course = course,
            CurrentLesson = lesson,
            Lessons = lessons,
            CompletedLessonIds = completedLessonIds,
            PreviousLesson = index > 0 ? lessons[index - 1] : null,
            NextLesson = index >= 0 && index < lessons.Count - 1 ? lessons[index + 1] : null
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkComplete(Guid lessonId, int watchedSeconds = 0)
    {
        var lessonResult = await _lessonService.GetLessonByIdAsync(lessonId);
        if (!lessonResult.IsSuccess || lessonResult.Data == null) return NotFound();

        var lesson = lessonResult.Data;
        var course = lesson.Section.Course;
        var studentId = Guid.Parse(User.FindFirst("UserId")!.Value);
        var enrollmentResult = await _enrollmentService.GetEnrollmentAsync(studentId, course.Id);
        if (!enrollmentResult.IsSuccess || enrollmentResult.Data == null) return Forbid();

        var enrollment = enrollmentResult.Data;
        if (enrollment.Status is not (EnrollmentStatus.Active or EnrollmentStatus.Completed)) return Forbid();

        var result = await _progressService.UpdateProgressAsync(enrollment.Id, lessonId, watchedSeconds, true);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Lesson marked complete." : result.ErrorMessage;

        var lessonsResult = await _lessonService.GetLessonsByCourseAsync(course.Id);
        var lessons = (lessonsResult.Data ?? Enumerable.Empty<DataLayer.Entities.Lesson>())
            .Where(l => l.IsPublished)
            .OrderBy(l => l.Section.Order)
            .ThenBy(l => l.Order)
            .ToList();
        var index = lessons.FindIndex(l => l.Id == lessonId);
        var nextLesson = index >= 0 && index < lessons.Count - 1 ? lessons[index + 1] : null;

        if (nextLesson != null)
            return RedirectToAction(nameof(Watch), new { id = nextLesson.Id });

        return RedirectToAction("Details", "Courses", new { id = course.Id });
    }
}
