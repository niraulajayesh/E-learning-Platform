using BusinessLayer.Interfaces;
using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using SharedLayer.Wrappers;

namespace BusinessLayer.Services;

public class LessonService : ILessonService
{
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _db;

    public LessonService(IUnitOfWork uow, AppDbContext db)
    {
        _uow = uow;
        _db = db;
    }

    public async Task<Result<Lesson>> GetLessonByIdAsync(Guid id, CancellationToken ct = default)
    {
        var lesson = await _uow.Lessons.Query()
            .Include(l => l.Quiz)
            .Include(l => l.Section).ThenInclude(s => s.Course).ThenInclude(c => c.Instructor)
            .Include(l => l.Section).ThenInclude(s => s.Course).ThenInclude(c => c.Category)
            .FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lesson == null) return Result<Lesson>.Failure("Lesson not found.");
        return Result<Lesson>.Success(lesson);
    }

    public async Task<Result<IEnumerable<Lesson>>> GetLessonsBySectionAsync(Guid sectionId, CancellationToken ct = default)
    {
        var lessons = await _uow.Lessons.GetBySectionAsync(sectionId, ct);
        return Result<IEnumerable<Lesson>>.Success(lessons);
    }

    public async Task<Result<IEnumerable<Lesson>>> GetLessonsByCourseAsync(Guid courseId, CancellationToken ct = default)
    {
        var lessons = await _uow.Lessons.GetByCourseAsync(courseId, ct);
        return Result<IEnumerable<Lesson>>.Success(lessons);
    }

    public async Task<Result<Lesson>> CreateLessonAsync(Lesson lesson, CancellationToken ct = default)
    {
        lesson.Order = await _uow.Lessons.GetMaxOrderInSectionAsync(lesson.SectionId, ct) + 1;
        await _uow.Lessons.AddAsync(lesson, ct);
        await _uow.SaveChangesAsync(ct);
        var courseId = await _uow.Lessons.Query().Where(l => l.Id == lesson.Id).Select(l => l.Section.CourseId).FirstAsync(ct);
        await _uow.Courses.UpdateAggregatesAsync(courseId, ct);
        await _uow.SaveChangesAsync(ct);
        return Result<Lesson>.Success(lesson);
    }

    public async Task<Result<Lesson>> CreateLessonForCourseAsync(Guid courseId, Lesson lesson, CancellationToken ct = default)
    {
        var course = await _uow.Courses.GetWithDetailsAsync(courseId, ct);
        if (course == null) return Result<Lesson>.Failure("Course not found.");

        var section = course.Sections.OrderBy(s => s.Order).FirstOrDefault();
        if (section == null)
        {
            section = new Section { CourseId = courseId, Title = "Course Content", Description = "Lessons for this course", Order = 1 };
        }

        lesson.Section = section;
        lesson.SectionId = section.Id;
        lesson.Order = section.Lessons.Any() ? section.Lessons.Max(l => l.Order) + 1 : 1;

        await _uow.Lessons.AddAsync(lesson, ct);
        await _uow.SaveChangesAsync(ct);
        await _uow.Courses.UpdateAggregatesAsync(courseId, ct);
        await _uow.SaveChangesAsync(ct);
        return Result<Lesson>.Success(lesson);
    }

    public async Task<Result> UpdateLessonAsync(Lesson lesson, CancellationToken ct = default)
    {
        _uow.Lessons.Update(lesson);
        await _uow.SaveChangesAsync(ct);
        var courseId = await _uow.Lessons.Query().Where(l => l.Id == lesson.Id).Select(l => l.Section.CourseId).FirstAsync(ct);
        await _uow.Courses.UpdateAggregatesAsync(courseId, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteLessonAsync(Guid id, CancellationToken ct = default)
    {
        var lesson = await _uow.Lessons.Query().Include(l => l.Section).FirstOrDefaultAsync(l => l.Id == id, ct);
        if (lesson == null) return Result.Failure("Lesson not found.");
        lesson.IsPublished = false;
        _uow.Lessons.Update(lesson);
        await _uow.SaveChangesAsync(ct);
        await _uow.Courses.UpdateAggregatesAsync(lesson.Section.CourseId, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ReorderLessonsAsync(Guid courseId, IReadOnlyList<Guid> lessonIds, CancellationToken ct = default)
    {
        var lessons = await _uow.Lessons.Query().Include(l => l.Section).Where(l => l.Section.CourseId == courseId && lessonIds.Contains(l.Id)).ToListAsync(ct);
        if (lessons.Count != lessonIds.Distinct().Count()) return Result.Failure("One or more lessons were not found for this course.");
        for (var i = 0; i < lessonIds.Count; i++) lessons.First(l => l.Id == lessonIds[i]).Order = i + 1;
        _uow.Lessons.UpdateRange(lessons);
        await _uow.SaveChangesAsync(ct);
        await _uow.Courses.UpdateAggregatesAsync(courseId, ct);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}

