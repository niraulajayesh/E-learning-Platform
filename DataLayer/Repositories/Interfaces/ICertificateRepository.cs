using DataLayer.Entities;

namespace DataLayer.Repositories.Interfaces;

/// <summary>
/// Certificate repository with public verification and student history queries.
/// </summary>
public interface ICertificateRepository : IRepository<Certificate>
{
    Task<Certificate?> GetByNumberAsync(string certificateNumber, CancellationToken ct = default);
    Task<Certificate?> GetByEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default);
    Task<IEnumerable<Certificate>> GetByStudentAsync(Guid studentId, CancellationToken ct = default);
    Task<IEnumerable<Certificate>> GetIssuedAsync(CancellationToken ct = default);
    Task<bool> ExistsByEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default);
}
