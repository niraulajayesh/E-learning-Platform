using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using SharedLayer.Enums;

namespace DataLayer.Repositories.Implementations;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower(), ct);

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken
            && u.RefreshTokenExpiry > DateTime.UtcNow, ct);

    public async Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(u => u.EmailVerificationToken == token, ct);

    public async Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(u =>
            u.PasswordResetToken == token &&
            u.PasswordResetTokenExpiry > DateTime.UtcNow, ct);

    public async Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken ct = default)
        => await _dbSet.AnyAsync(u =>
            u.Email.ToLower() == email.ToLower() &&
            (excludeId == null || u.Id != excludeId), ct);

    public async Task<IEnumerable<User>> GetInstructorsAsync(CancellationToken ct = default)
        => await _dbSet
            .Where(u => u.Role == UserRole.Instructor && u.IsActive)
            .OrderBy(u => u.FirstName)
            .ToListAsync(ct);

    public async Task<IEnumerable<User>> GetAllStudentsAsync(CancellationToken ct = default)
        => await _dbSet
            .Where(u => u.Role == UserRole.Student)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync(ct);
}


