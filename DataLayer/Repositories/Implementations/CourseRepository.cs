using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using SharedLayer.Enums;

namespace DataLayer.Repositories.Implementations;

public class CourseRepository : Repository<Course>, ICourseRepository
{
    public CourseRepository(AppDbContext context) : base(context) { }

    public async Task<Course?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => await _dbSet.Include(c => c.Instructor).Include(c => c.Category).FirstOrDefaultAsync(c => c.Slug == slug, ct);

    public async Task<Course?> GetWithDetailsAsync(Guid courseId, CancellationToken ct = default)
        => await _dbSet
            .Include(c => c.Instructor)
            .Include(c => c.Category)
            .Include(c => c.Sections.OrderBy(s => s.Order))
                .ThenInclude(s => s.Lessons.OrderBy(l => l.Order))
            .FirstOrDefaultAsync(c => c.Id == courseId, ct);

    public async Task<IEnumerable<Course>> GetPublishedAsync(CancellationToken ct = default)
        => await _dbSet.Where(c => c.Status == CourseStatus.Published).Include(c => c.Instructor).Include(c => c.Category).OrderByDescending(c => c.PublishedAt).ToListAsync(ct);

    public async Task<IEnumerable<Course>> GetByInstructorAsync(Guid instructorId, CancellationToken ct = default)
        => await _dbSet.Where(c => c.InstructorId == instructorId).Include(c => c.Category).OrderByDescending(c => c.CreatedAt).ToListAsync(ct);

    public async Task<IEnumerable<Course>> GetByCategoryAsync(int categoryId, CancellationToken ct = default)
        => await _dbSet.Where(c => c.CategoryId == categoryId && c.Status == CourseStatus.Published).Include(c => c.Instructor).OrderByDescending(c => c.TotalEnrollments).ToListAsync(ct);

    public async Task<IEnumerable<Course>> GetFeaturedAsync(int count = 6, CancellationToken ct = default)
        => await _dbSet.Where(c => c.IsFeatured && c.Status == CourseStatus.Published).Include(c => c.Instructor).Include(c => c.Category).OrderByDescending(c => c.AverageRating).Take(count).ToListAsync(ct);

    public async Task<IEnumerable<Course>> SearchAsync(string keyword, CancellationToken ct = default)
    {
        var lower = keyword.ToLower();
        return await _dbSet
            .Where(c => c.Status == CourseStatus.Published && (c.Title.ToLower().Contains(lower) || c.ShortDescription.ToLower().Contains(lower) || c.Description.ToLower().Contains(lower)))
            .Include(c => c.Instructor)
            .Include(c => c.Category)
            .OrderByDescending(c => c.TotalEnrollments)
            .ToListAsync(ct);
    }

    public async Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken ct = default)
        => await _dbSet.AnyAsync(c => c.Slug == slug && (excludeId == null || c.Id != excludeId), ct);

    public async Task UpdateAggregatesAsync(Guid courseId, CancellationToken ct = default)
    {
        var course = await _dbSet.Include(c => c.Reviews).Include(c => c.Enrollments).Include(c => c.Sections).ThenInclude(s => s.Lessons).FirstOrDefaultAsync(c => c.Id == courseId, ct);
        if (course is null) return;

        course.TotalEnrollments = course.Enrollments.Count;
        course.TotalReviews = course.Reviews.Count(r => r.IsVisible);
        course.AverageRating = course.Reviews.Any(r => r.IsVisible) ? Math.Round(course.Reviews.Where(r => r.IsVisible).Average(r => r.Rating), 1) : 0.0;
        course.TotalLessons = course.Sections.SelectMany(s => s.Lessons).Count();
        course.TotalDurationMinutes = course.Sections.SelectMany(s => s.Lessons).Sum(l => l.DurationMinutes);

        _dbSet.Update(course);
    }
}
