using BusinessLayer.Interfaces;
using DataLayer.Entities;
using DataLayer.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using SharedLayer.Enums;
using SharedLayer.Wrappers;

namespace BusinessLayer.Services;

public class CourseService : ICourseService
{
    private readonly IUnitOfWork _uow;

    public CourseService(IUnitOfWork uow) => _uow = uow;

    public async Task<Result<Course>> CreateCourseAsync(Course course, CancellationToken ct = default)
    {
        Normalize(course);
        if (await _uow.Courses.SlugExistsAsync(course.Slug, null, ct)) return Result<Course>.Failure("Course slug already exists.");
        if (course.Status == CourseStatus.Published && course.PublishedAt == null) course.PublishedAt = DateTime.UtcNow;
        await _uow.Courses.AddAsync(course, ct);
        await _uow.SaveChangesAsync(ct);
        return Result<Course>.Success(course);
    }

    public async Task<Result<Course>> GetCourseByIdAsync(Guid id, CancellationToken ct = default)
    {
        var course = await _uow.Courses.GetWithDetailsAsync(id, ct);
        if (course == null) return Result<Course>.Failure("Course not found.");
        return Result<Course>.Success(course);
    }

    public async Task<Result<Course>> GetCourseBySlugAsync(string slug, CancellationToken ct = default)
    {
        var course = await _uow.Courses.GetBySlugAsync(slug, ct);
        if (course == null) return Result<Course>.Failure("Course not found.");
        return Result<Course>.Success(course);
    }

    public async Task<Result<IEnumerable<Course>>> GetAllCoursesAsync(CancellationToken ct = default)
    {
        var courses = await _uow.Courses.Query().Include(c => c.Instructor).Include(c => c.Category).OrderByDescending(c => c.CreatedAt).ToListAsync(ct);
        return Result<IEnumerable<Course>>.Success(courses);
    }

    public async Task<Result<IEnumerable<Course>>> GetAllPublishedCoursesAsync(CancellationToken ct = default)
    {
        var courses = await _uow.Courses.GetPublishedAsync(ct);
        return Result<IEnumerable<Course>>.Success(courses);
    }

    public async Task<Result<IEnumerable<Course>>> GetCoursesByInstructorAsync(Guid instructorId, CancellationToken ct = default)
    {
        var courses = await _uow.Courses.GetByInstructorAsync(instructorId, ct);
        return Result<IEnumerable<Course>>.Success(courses);
    }

    public async Task<Result<IEnumerable<Course>>> SearchCoursesAsync(string keyword, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return await GetAllPublishedCoursesAsync(ct);
        var courses = await _uow.Courses.SearchAsync(keyword, ct);
        return Result<IEnumerable<Course>>.Success(courses);
    }

    public async Task<Result> UpdateCourseAsync(Course course, CancellationToken ct = default)
    {
        Normalize(course);
        if (await _uow.Courses.SlugExistsAsync(course.Slug, course.Id, ct)) return Result.Failure("Course slug already exists.");
        if (course.Status == CourseStatus.Published && course.PublishedAt == null) course.PublishedAt = DateTime.UtcNow;
        if (course.Status != CourseStatus.Published) course.PublishedAt = null;
        _uow.Courses.Update(course);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteCourseAsync(Guid id, CancellationToken ct = default)
    {
        var course = await _uow.Courses.Query().Include(c => c.Sections).ThenInclude(s => s.Lessons).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (course == null) return Result.Failure("Course not found.");
        course.Status = CourseStatus.Archived;
        course.IsFeatured = false;
        foreach (var lesson in course.Sections.SelectMany(s => s.Lessons)) lesson.IsPublished = false;
        _uow.Courses.Update(course);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> PublishCourseAsync(Guid id, CancellationToken ct = default)
    {
        var course = await _uow.Courses.GetByIdAsync(id, ct);
        if (course == null) return Result.Failure("Course not found.");
        course.Status = CourseStatus.Published;
        course.PublishedAt = DateTime.UtcNow;
        _uow.Courses.Update(course);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> UnpublishCourseAsync(Guid id, CancellationToken ct = default)
    {
        var course = await _uow.Courses.GetByIdAsync(id, ct);
        if (course == null) return Result.Failure("Course not found.");
        course.Status = CourseStatus.Draft;
        course.PublishedAt = null;
        _uow.Courses.Update(course);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ToggleFeaturedAsync(Guid id, CancellationToken ct = default)
    {
        var course = await _uow.Courses.GetByIdAsync(id, ct);
        if (course == null) return Result.Failure("Course not found.");
        course.IsFeatured = !course.IsFeatured;
        _uow.Courses.Update(course);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static void Normalize(Course course)
    {
        course.Title = course.Title.Trim();
        if (string.IsNullOrWhiteSpace(course.Slug)) course.Slug = Slugify(course.Title);
        course.Slug = Slugify(course.Slug);
        course.Language = string.IsNullOrWhiteSpace(course.Language) ? "English" : course.Language.Trim();
    }

    private static string Slugify(string value)
    {
        var chars = value.Trim().ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray();
        var slug = string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N") : slug;
    }
}
