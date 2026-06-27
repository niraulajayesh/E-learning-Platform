using DataLayer.Entities;

namespace DataLayer.Repositories.Interfaces;

/// <summary>
/// Payment repository for transaction history and gateway reconciliation.
/// </summary>
public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetByGatewayReferenceAsync(string reference, CancellationToken ct = default);
    Task<IEnumerable<Payment>> GetByStudentAsync(Guid studentId, CancellationToken ct = default);
    Task<IEnumerable<Payment>> GetByCourseAsync(Guid courseId, CancellationToken ct = default);
    Task<decimal> GetTotalRevenueAsync(CancellationToken ct = default);
    Task<decimal> GetInstructorRevenueAsync(Guid instructorId, CancellationToken ct = default);
}
