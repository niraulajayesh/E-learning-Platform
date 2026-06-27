using DataLayer.Entities;
using SharedLayer.Wrappers;

namespace BusinessLayer.Interfaces;

public interface IUserService
{
    Task<Result<User>> RegisterStudentAsync(string firstName, string lastName, string email, string passwordHash, CancellationToken ct = default);
    Task<Result<User>> CreateUserAsync(User user, string passwordHash, CancellationToken ct = default);
    Task<Result<User>> GetUserByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<User>> GetUserByEmailAsync(string email, CancellationToken ct = default);
    Task<Result<IEnumerable<User>>> GetAllUsersAsync(CancellationToken ct = default);
    Task<Result<IEnumerable<User>>> GetAllInstructorsAsync(CancellationToken ct = default);
    Task<Result> UpdateProfileAsync(User user, CancellationToken ct = default);
    Task<Result> SetActiveStatusAsync(Guid id, bool isActive, CancellationToken ct = default);
    Task<Result> SetRoleAsync(Guid id, SharedLayer.Enums.UserRole role, CancellationToken ct = default);
    Task<Result> ResetPasswordAsync(Guid id, string passwordHash, CancellationToken ct = default);
}
