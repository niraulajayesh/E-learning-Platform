using DataLayer.Entities;
using SharedLayer.Wrappers;

namespace BusinessLayer.Interfaces;

public interface ILessonService
{
    Task<Result<Lesson>> GetLessonByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<IEnumerable<Lesson>>> GetLessonsBySectionAsync(Guid sectionId, CancellationToken ct = default);
    Task<Result<IEnumerable<Lesson>>> GetLessonsByCourseAsync(Guid courseId, CancellationToken ct = default);
    Task<Result<Lesson>> CreateLessonAsync(Lesson lesson, CancellationToken ct = default);
    Task<Result<Lesson>> CreateLessonForCourseAsync(Guid courseId, Lesson lesson, CancellationToken ct = default);
    Task<Result> UpdateLessonAsync(Lesson lesson, CancellationToken ct = default);
    Task<Result> DeleteLessonAsync(Guid id, CancellationToken ct = default);
    Task<Result> ReorderLessonsAsync(Guid courseId, IReadOnlyList<Guid> lessonIds, CancellationToken ct = default);
}
