using DataLayer.Entities;

namespace DataLayer.Repositories.Interfaces;

/// <summary>
/// Course-specific repository with catalog, search, and instructor queries.
/// </summary>
public interface ICourseRepository : IRepository<Course>
{
    Task<Course?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Course?> GetWithDetailsAsync(Guid courseId, CancellationToken ct = default);  // includes sections, lessons
    Task<IEnumerable<Course>> GetPublishedAsync(CancellationToken ct = default);
    Task<IEnumerable<Course>> GetByInstructorAsync(Guid instructorId, CancellationToken ct = default);
    Task<IEnumerable<Course>> GetByCategoryAsync(int categoryId, CancellationToken ct = default);
    Task<IEnumerable<Course>> GetFeaturedAsync(int count = 6, CancellationToken ct = default);
    Task<IEnumerable<Course>> SearchAsync(string keyword, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default);
    Task UpdateAggregatesAsync(Guid courseId, CancellationToken ct = default);   // refresh rating, enrollment count
}
