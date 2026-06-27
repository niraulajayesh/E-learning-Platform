using DataLayer.Entities;
using SharedLayer.Wrappers;

namespace BusinessLayer.Interfaces;

public interface IReviewService
{
    Task<Result<Review>> AddReviewAsync(Guid courseId, Guid studentId, int rating, string? comment, CancellationToken ct = default);
    Task<Result<IEnumerable<Review>>> GetCourseReviewsAsync(Guid courseId, CancellationToken ct = default);
    Task<Result> DeleteReviewAsync(Guid reviewId, CancellationToken ct = default);
}
