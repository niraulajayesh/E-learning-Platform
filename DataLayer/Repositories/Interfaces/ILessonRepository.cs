using DataLayer.Entities;

namespace DataLayer.Repositories.Interfaces;

/// <summary>
/// Lesson and Section repository with ordering and content type queries.
/// </summary>
public interface ILessonRepository : IRepository<Lesson>
{
    Task<Lesson?> GetWithQuizAsync(Guid lessonId, CancellationToken ct = default);
    Task<IEnumerable<Lesson>> GetBySectionAsync(Guid sectionId, CancellationToken ct = default);
    Task<IEnumerable<Lesson>> GetByCourseAsync(Guid courseId, CancellationToken ct = default);
    Task<int> GetMaxOrderInSectionAsync(Guid sectionId, CancellationToken ct = default);
}
