using DataLayer.Entities;

namespace DataLayer.Repositories.Interfaces;

/// <summary>
/// Enrollment-specific repository for student ↔ course access checks and reporting.
/// </summary>
public interface IEnrollmentRepository : IRepository<Enrollment>
{
    Task<Enrollment?> GetByStudentAndCourseAsync(Guid studentId, Guid courseId, CancellationToken ct = default);
    Task<Enrollment?> GetWithProgressAsync(Guid enrollmentId, CancellationToken ct = default);
    Task<IEnumerable<Enrollment>> GetByStudentAsync(Guid studentId, CancellationToken ct = default);
    Task<IEnumerable<Enrollment>> GetByCourseAsync(Guid courseId, CancellationToken ct = default);
    Task<bool> IsEnrolledAsync(Guid studentId, Guid courseId, CancellationToken ct = default);
    Task<int> GetEnrollmentCountAsync(Guid courseId, CancellationToken ct = default);
}
