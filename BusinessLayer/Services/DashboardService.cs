using BusinessLayer.Interfaces;
using DataLayer.UnitOfWork;
using SharedLayer.Enums;
using SharedLayer.Wrappers;

namespace BusinessLayer.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _uow;

    public DashboardService(IUnitOfWork uow) => _uow = uow;

    public async Task<Result<object>> GetAdminDashboardMetricsAsync(CancellationToken ct = default)
    {
        var totalUsers = await _uow.Users.CountAsync(null, ct);
        var totalStudents = await _uow.Users.CountAsync(u => u.Role == UserRole.Student, ct);
        var totalInstructors = await _uow.Users.CountAsync(u => u.Role == UserRole.Instructor, ct);
        var totalCourses = await _uow.Courses.CountAsync(null, ct);
        var totalRevenue = await _uow.Payments.GetTotalRevenueAsync(ct);
        var activeEnrollments = await _uow.Enrollments.CountAsync(e => e.Status == EnrollmentStatus.Active, ct);
        var completedEnrollments = await _uow.Enrollments.CountAsync(e => e.Status == EnrollmentStatus.Completed, ct);
        var totalEnrollments = activeEnrollments + completedEnrollments + await _uow.Enrollments.CountAsync(e => e.Status != EnrollmentStatus.Active && e.Status != EnrollmentStatus.Completed, ct);
        var attempts = _uow.Quizzes.Query().SelectMany(q => q.Attempts);
        var attemptCount = attempts.Count();
        var passedAttempts = attempts.Count(a => a.IsPassed);

        return Result<object>.Success(new
        {
            TotalUsers = totalUsers,
            TotalStudents = totalStudents,
            TotalInstructors = totalInstructors,
            TotalCourses = totalCourses,
            TotalRevenue = totalRevenue,
            ActiveEnrollments = activeEnrollments,
            TotalEnrollments = totalEnrollments,
            CourseCompletionRate = totalEnrollments == 0 ? 0 : Math.Round(completedEnrollments * 100.0 / totalEnrollments, 1),
            QuizPassRate = attemptCount == 0 ? 0 : Math.Round(passedAttempts * 100.0 / attemptCount, 1)
        });
    }

    public async Task<Result<object>> GetInstructorDashboardMetricsAsync(Guid instructorId, CancellationToken ct = default)
    {
        var courses = await _uow.Courses.GetByInstructorAsync(instructorId, ct);
        var totalRevenue = await _uow.Payments.GetInstructorRevenueAsync(instructorId, ct);
        return Result<object>.Success(new { TotalCourses = courses.Count(), TotalStudents = courses.Sum(c => c.TotalEnrollments), TotalRevenue = totalRevenue });
    }

    public async Task<Result<object>> GetStudentDashboardMetricsAsync(Guid studentId, CancellationToken ct = default)
    {
        var enrollments = await _uow.Enrollments.GetByStudentAsync(studentId, ct);
        var totalCertificates = await _uow.Certificates.CountAsync(c => c.StudentId == studentId, ct);
        return Result<object>.Success(new { ActiveCourses = enrollments.Count(e => e.Status == EnrollmentStatus.Active), CompletedCourses = enrollments.Count(e => e.Status == EnrollmentStatus.Completed), TotalCertificates = totalCertificates });
    }
}
