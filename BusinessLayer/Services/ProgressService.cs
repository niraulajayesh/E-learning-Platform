using BusinessLayer.Interfaces;
using DataLayer.Entities;
using DataLayer.UnitOfWork;
using SharedLayer.Enums;
using SharedLayer.Wrappers;

namespace BusinessLayer.Services;

public class ProgressService : IProgressService
{
    private readonly IUnitOfWork _uow;

    public ProgressService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result> UpdateProgressAsync(Guid enrollmentId, Guid lessonId, int watchedSeconds, bool isCompleted, CancellationToken ct = default)
    {
        var enrollment = await _uow.Enrollments.GetByIdAsync(enrollmentId, ct);
        if (enrollment == null) return Result.Failure("Enrollment not found.");

        var progress = await _uow.Progress.GetFirstOrDefaultAsync(p => p.EnrollmentId == enrollmentId && p.LessonId == lessonId, ct);

        if (progress == null)
        {
            progress = new Progress
            {
                EnrollmentId = enrollmentId,
                LessonId = lessonId,
                WatchedSeconds = watchedSeconds,
                IsCompleted = isCompleted,
                CompletedAt = isCompleted ? DateTime.UtcNow : null,
                LastWatchedAt = DateTime.UtcNow
            };
            await _uow.Progress.AddAsync(progress, ct);
        }
        else
        {
            progress.WatchedSeconds = Math.Max(progress.WatchedSeconds, watchedSeconds);
            progress.LastWatchedAt = DateTime.UtcNow;
            if (isCompleted && !progress.IsCompleted)
            {
                progress.IsCompleted = true;
                progress.CompletedAt = DateTime.UtcNow;
            }
            _uow.Progress.Update(progress);
        }

        enrollment.LastAccessedAt = DateTime.UtcNow;
        _uow.Enrollments.Update(enrollment);

        await _uow.SaveChangesAsync(ct);
        await RecalculateEnrollmentCompletionAsync(enrollmentId, ct);

        return Result.Success();
    }

    public async Task<Result<IReadOnlySet<Guid>>> GetCompletedLessonIdsAsync(Guid enrollmentId, CancellationToken ct = default)
    {
        var progress = await _uow.Progress.FindAsync(p => p.EnrollmentId == enrollmentId && p.IsCompleted, ct);
        return Result<IReadOnlySet<Guid>>.Success(progress.Select(p => p.LessonId).ToHashSet());
    }

    public async Task<Result<double>> GetCourseCompletionPercentageAsync(Guid enrollmentId, CancellationToken ct = default)
    {
        var enrollment = await _uow.Enrollments.GetByIdAsync(enrollmentId, ct);
        if (enrollment == null) return Result<double>.Failure("Enrollment not found.");
        return Result<double>.Success(enrollment.CompletionPercentage);
    }

    private async Task RecalculateEnrollmentCompletionAsync(Guid enrollmentId, CancellationToken ct)
    {
        var enrollment = await _uow.Enrollments.GetWithProgressAsync(enrollmentId, ct);
        if (enrollment == null) return;

        var course = await _uow.Courses.GetWithDetailsAsync(enrollment.CourseId, ct);
        if (course == null) return;

        var totalLessons = course.Sections.SelectMany(s => s.Lessons).Count(l => l.IsPublished);
        if (totalLessons == 0) return;

        var courseLessonIds = course.Sections.SelectMany(s => s.Lessons).Where(l => l.IsPublished).Select(l => l.Id).ToHashSet();
        var completedLessons = enrollment.ProgressRecords.Count(p => p.IsCompleted && courseLessonIds.Contains(p.LessonId));
        enrollment.CompletionPercentage = Math.Round((double)completedLessons / totalLessons * 100, 2);

        if (enrollment.CompletionPercentage >= 100 && enrollment.Status == EnrollmentStatus.Active)
        {
            enrollment.Status = EnrollmentStatus.Completed;
            enrollment.CompletedAt = DateTime.UtcNow;
        }

        _uow.Enrollments.Update(enrollment);
        await _uow.SaveChangesAsync(ct);
    }
}
