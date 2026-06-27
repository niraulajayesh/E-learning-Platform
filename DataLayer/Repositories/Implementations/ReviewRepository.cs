using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Repositories.Implementations;

public class ReviewRepository : Repository<Review>, IReviewRepository
{
    public ReviewRepository(AppDbContext context) : base(context) { }

    public async Task<Review?> GetByStudentAndCourseAsync(Guid studentId, Guid courseId, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(r =>
            r.StudentId == studentId && r.CourseId == courseId, ct);

    public async Task<IEnumerable<Review>> GetByCourseAsync(Guid courseId, bool visibleOnly = true, CancellationToken ct = default)
        => await _dbSet
            .Where(r => r.CourseId == courseId && (!visibleOnly || r.IsVisible))
            .Include(r => r.Student)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task<double> GetAverageRatingAsync(Guid courseId, CancellationToken ct = default)
    {
        var avg = await _dbSet
            .Where(r => r.CourseId == courseId && r.IsVisible)
            .AverageAsync(r => (double?)r.Rating, ct);
        return avg.HasValue ? Math.Round(avg.Value, 1) : 0.0;
    }

    public async Task<int> GetReviewCountAsync(Guid courseId, CancellationToken ct = default)
        => await _dbSet.CountAsync(r => r.CourseId == courseId && r.IsVisible, ct);
}
