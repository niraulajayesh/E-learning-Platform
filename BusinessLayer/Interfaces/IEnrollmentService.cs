using DataLayer.Entities;
using SharedLayer.Wrappers;

namespace BusinessLayer.Interfaces;

public interface IEnrollmentService
{
    Task<Result<Enrollment>> EnrollStudentAsync(Guid studentId, Guid courseId, CancellationToken ct = default);
    Task<Result<Enrollment>> GetEnrollmentAsync(Guid studentId, Guid courseId, CancellationToken ct = default);
    Task<Result<IEnumerable<Enrollment>>> GetStudentEnrollmentsAsync(Guid studentId, CancellationToken ct = default);
    Task<Result> CancelEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default);
    Task<Result<bool>> IsStudentEnrolledAsync(Guid studentId, Guid courseId, CancellationToken ct = default);
}
