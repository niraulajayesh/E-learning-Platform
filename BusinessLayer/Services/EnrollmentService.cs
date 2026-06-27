using BusinessLayer.Interfaces;
using DataLayer.Entities;
using DataLayer.UnitOfWork;
using SharedLayer.Enums;
using SharedLayer.Wrappers;

namespace BusinessLayer.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IUnitOfWork _uow;

    public EnrollmentService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<Enrollment>> EnrollStudentAsync(Guid studentId, Guid courseId, CancellationToken ct = default)
    {
        var student = await _uow.Users.GetByIdAsync(studentId, ct);
        if (student == null || !student.IsActive || student.Role != UserRole.Student)
            return Result<Enrollment>.Failure("Your sign-in session is no longer valid. Please sign in again.");

        var course = await _uow.Courses.GetByIdAsync(courseId, ct);
        if (course == null)
            return Result<Enrollment>.Failure("Course not found.");

        if (await _uow.Enrollments.IsEnrolledAsync(studentId, courseId, ct))
            return Result<Enrollment>.Failure("Student is already enrolled in this course.");

        var enrollment = new Enrollment
        {
            StudentId = studentId,
            CourseId = courseId,
            Status = EnrollmentStatus.Active,
            EnrolledAt = DateTime.UtcNow
        };

        await _uow.Enrollments.AddAsync(enrollment, ct);
        
        course.TotalEnrollments++;
        _uow.Courses.Update(course);

        await _uow.SaveChangesAsync(ct);
        return Result<Enrollment>.Success(enrollment);
    }

    public async Task<Result<Enrollment>> GetEnrollmentAsync(Guid studentId, Guid courseId, CancellationToken ct = default)
    {
        var enrollment = await _uow.Enrollments.GetByStudentAndCourseAsync(studentId, courseId, ct);
        if (enrollment == null) return Result<Enrollment>.Failure("Enrollment not found.");
        return Result<Enrollment>.Success(enrollment);
    }

    public async Task<Result<IEnumerable<Enrollment>>> GetStudentEnrollmentsAsync(Guid studentId, CancellationToken ct = default)
    {
        var enrollments = await _uow.Enrollments.GetByStudentAsync(studentId, ct);
        return Result<IEnumerable<Enrollment>>.Success(enrollments);
    }

    public async Task<Result> CancelEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default)
    {
        var enrollment = await _uow.Enrollments.GetByIdAsync(enrollmentId, ct);
        if (enrollment == null) return Result.Failure("Enrollment not found.");

        enrollment.Status = EnrollmentStatus.Cancelled;
        _uow.Enrollments.Update(enrollment);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<bool>> IsStudentEnrolledAsync(Guid studentId, Guid courseId, CancellationToken ct = default)
    {
        var isEnrolled = await _uow.Enrollments.IsEnrolledAsync(studentId, courseId, ct);
        return Result<bool>.Success(isEnrolled);
    }
}


