using DataLayer.Entities;
using SharedLayer.Wrappers;

namespace BusinessLayer.Interfaces;

public interface ICourseService
{
    Task<Result<Course>> CreateCourseAsync(Course course, CancellationToken ct = default);
    Task<Result<Course>> GetCourseByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<Course>> GetCourseBySlugAsync(string slug, CancellationToken ct = default);
    Task<Result<IEnumerable<Course>>> GetAllCoursesAsync(CancellationToken ct = default);
    Task<Result<IEnumerable<Course>>> GetAllPublishedCoursesAsync(CancellationToken ct = default);
    Task<Result<IEnumerable<Course>>> GetCoursesByInstructorAsync(Guid instructorId, CancellationToken ct = default);
    Task<Result<IEnumerable<Course>>> SearchCoursesAsync(string keyword, CancellationToken ct = default);
    Task<Result> UpdateCourseAsync(Course course, CancellationToken ct = default);
    Task<Result> DeleteCourseAsync(Guid id, CancellationToken ct = default);
    Task<Result> PublishCourseAsync(Guid id, CancellationToken ct = default);
    Task<Result> UnpublishCourseAsync(Guid id, CancellationToken ct = default);
    Task<Result> ToggleFeaturedAsync(Guid id, CancellationToken ct = default);
}
