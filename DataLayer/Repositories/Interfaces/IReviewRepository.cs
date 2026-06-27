using DataLayer.Entities;

namespace DataLayer.Repositories.Interfaces;

/// <summary>
/// Review repository with rating aggregation and visibility filtering.
/// </summary>
public interface IReviewRepository : IRepository<Review>
{
    Task<Review?> GetByStudentAndCourseAsync(Guid studentId, Guid courseId, CancellationToken ct = default);
    Task<IEnumerable<Review>> GetByCourseAsync(Guid courseId, bool visibleOnly = true, CancellationToken ct = default);
    Task<double> GetAverageRatingAsync(Guid courseId, CancellationToken ct = default);
    Task<int> GetReviewCountAsync(Guid courseId, CancellationToken ct = default);
}
