using BusinessLayer.Interfaces;
using DataLayer.Entities;
using DataLayer.UnitOfWork;
using SharedLayer.Wrappers;

namespace BusinessLayer.Services;

public class ReviewService : IReviewService
{
    private readonly IUnitOfWork _uow;

    public ReviewService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<Review>> AddReviewAsync(Guid courseId, Guid studentId, int rating, string? comment, CancellationToken ct = default)
    {
        if (rating < 1 || rating > 5)
            return Result<Review>.Failure("Rating must be between 1 and 5.");

        var existingReview = await _uow.Reviews.GetByStudentAndCourseAsync(studentId, courseId, ct);
        if (existingReview != null)
            return Result<Review>.Failure("You have already reviewed this course.");

        if (!await _uow.Enrollments.IsEnrolledAsync(studentId, courseId, ct))
            return Result<Review>.Failure("You must be enrolled to review this course.");

        var review = new Review
        {
            CourseId = courseId,
            StudentId = studentId,
            Rating = rating,
            Comment = comment,
            IsVisible = true,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Reviews.AddAsync(review, ct);
        await _uow.SaveChangesAsync(ct);

        await _uow.Courses.UpdateAggregatesAsync(courseId, ct);

        return Result<Review>.Success(review);
    }

    public async Task<Result<IEnumerable<Review>>> GetCourseReviewsAsync(Guid courseId, CancellationToken ct = default)
    {
        var reviews = await _uow.Reviews.GetByCourseAsync(courseId, true, ct);
        return Result<IEnumerable<Review>>.Success(reviews);
    }

    public async Task<Result> DeleteReviewAsync(Guid reviewId, CancellationToken ct = default)
    {
        var review = await _uow.Reviews.GetByIdAsync(reviewId, ct);
        if (review == null) return Result.Failure("Review not found.");

        var courseId = review.CourseId;
        _uow.Reviews.Remove(review);
        await _uow.SaveChangesAsync(ct);

        await _uow.Courses.UpdateAggregatesAsync(courseId, ct);
        return Result.Success();
    }
}
