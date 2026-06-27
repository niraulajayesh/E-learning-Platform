using DataLayer.Entities;

namespace DataLayer.Repositories.Interfaces;

/// <summary>
/// User-specific repository with authentication and profile queries.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken ct = default);
    Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken ct = default);
    Task<IEnumerable<User>> GetInstructorsAsync(CancellationToken ct = default);
    Task<IEnumerable<User>> GetAllStudentsAsync(CancellationToken ct = default);
}
