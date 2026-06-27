using System.Text.Json;
using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedLayer.Enums;
using UserLayer.Models;

namespace UserLayer.Controllers;

[Authorize]
public class QuizzesController : Controller
{
    private readonly IQuizService _quizService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IProgressService _progressService;

    public QuizzesController(IQuizService quizService, IEnrollmentService enrollmentService, IProgressService progressService)
    {
        _quizService = quizService;
        _enrollmentService = enrollmentService;
        _progressService = progressService;
    }

    public async Task<IActionResult> Take(Guid id)
    {
        var quizResult = await _quizService.GetQuizByIdAsync(id);
        if (!quizResult.IsSuccess || quizResult.Data == null) return NotFound();

        var quiz = quizResult.Data;
        if (!quiz.IsPublished) return NotFound();
        var studentId = Guid.Parse(User.FindFirst("UserId")!.Value);
        var enrollmentResult = await _enrollmentService.GetEnrollmentAsync(studentId, quiz.Lesson.Section.CourseId);
        if (!enrollmentResult.IsSuccess || enrollmentResult.Data == null) return Forbid();
        if (enrollmentResult.Data.Status is not (EnrollmentStatus.Active or EnrollmentStatus.Completed)) return Forbid();

        var attempts = (await _quizService.GetStudentQuizAttemptsAsync(id, studentId)).Data?.ToList() ?? new List<DataLayer.Entities.QuizAttempt>();
        var canAttempt = quiz.MaxAttempts == 0 || attempts.Count < quiz.MaxAttempts;

        ViewData["Title"] = quiz.Title;
        return View(new QuizTakeViewModel
        {
            Quiz = quiz,
            Attempts = attempts,
            CanAttempt = canAttempt,
            LockReason = canAttempt ? null : "Maximum number of attempts reached."
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid quizId, int timeTakenSeconds = 0)
    {
        var quizResult = await _quizService.GetQuizByIdAsync(quizId);
        if (!quizResult.IsSuccess || quizResult.Data == null) return NotFound();

        var quiz = quizResult.Data;
        if (!quiz.IsPublished) return NotFound();
        var studentId = Guid.Parse(User.FindFirst("UserId")!.Value);
        var enrollmentResult = await _enrollmentService.GetEnrollmentAsync(studentId, quiz.Lesson.Section.CourseId);
        if (!enrollmentResult.IsSuccess || enrollmentResult.Data == null) return Forbid();
        if (enrollmentResult.Data.Status is not (EnrollmentStatus.Active or EnrollmentStatus.Completed)) return Forbid();

        var answers = new Dictionary<Guid, Guid>();
        foreach (var question in quiz.Questions)
        {
            var value = Request.Form[$"answers[{question.Id}]"].FirstOrDefault();
            if (Guid.TryParse(value, out var answerId)) answers[question.Id] = answerId;
        }

        var result = await _quizService.SubmitQuizAsync(quizId, studentId, answers, timeTakenSeconds);
        if (!result.IsSuccess || result.Data == null)
        {
            TempData["Error"] = result.ErrorMessage;
            return RedirectToAction(nameof(Take), new { id = quizId });
        }

        if (result.Data.IsPassed)
            await _progressService.UpdateProgressAsync(enrollmentResult.Data.Id, quiz.LessonId, quiz.Lesson.DurationMinutes * 60, true);

        return RedirectToAction(nameof(Result), new { id = result.Data.Id });
    }

    public async Task<IActionResult> Result(Guid id)
    {
        var studentId = Guid.Parse(User.FindFirst("UserId")!.Value);
        var attempts = (await _quizService.GetStudentQuizAttemptsAsync(studentId)).Data?.ToList() ?? new List<DataLayer.Entities.QuizAttempt>();
        var attempt = attempts.FirstOrDefault(a => a.Id == id);
        if (attempt == null) return NotFound();

        var quizResult = await _quizService.GetQuizByIdAsync(attempt.QuizId);
        if (!quizResult.IsSuccess || quizResult.Data == null) return NotFound();

        var selected = string.IsNullOrWhiteSpace(attempt.AnswersSnapshot)
            ? new Dictionary<Guid, Guid>()
            : JsonSerializer.Deserialize<Dictionary<Guid, Guid>>(attempt.AnswersSnapshot) ?? new Dictionary<Guid, Guid>();

        ViewData["Title"] = "Quiz Result";
        return View(new QuizResultViewModel
        {
            Quiz = quizResult.Data,
            Attempt = attempt,
            Attempts = attempts.Where(a => a.QuizId == attempt.QuizId).ToList(),
            SelectedAnswers = selected
        });
    }
}


