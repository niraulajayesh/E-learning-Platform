using SharedLayer.Wrappers;

namespace BusinessLayer.Interfaces;

public interface IProgressService
{
    Task<Result> UpdateProgressAsync(Guid enrollmentId, Guid lessonId, int watchedSeconds, bool isCompleted, CancellationToken ct = default);
    Task<Result<IReadOnlySet<Guid>>> GetCompletedLessonIdsAsync(Guid enrollmentId, CancellationToken ct = default);
    Task<Result<double>> GetCourseCompletionPercentageAsync(Guid enrollmentId, CancellationToken ct = default);
}
