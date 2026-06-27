using SharedLayer.Wrappers;

namespace BusinessLayer.Interfaces;

public interface IDashboardService
{
    Task<Result<object>> GetAdminDashboardMetricsAsync(CancellationToken ct = default);
    Task<Result<object>> GetInstructorDashboardMetricsAsync(Guid instructorId, CancellationToken ct = default);
    Task<Result<object>> GetStudentDashboardMetricsAsync(Guid studentId, CancellationToken ct = default);
}
