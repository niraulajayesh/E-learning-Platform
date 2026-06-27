using BusinessLayer.Interfaces;
using DataLayer.Entities;
using DataLayer.UnitOfWork;
using SharedLayer.Enums;
using SharedLayer.Wrappers;

namespace BusinessLayer.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;

    public UserService(IUnitOfWork uow) => _uow = uow;

    public async Task<Result<User>> RegisterStudentAsync(string firstName, string lastName, string email, string passwordHash, CancellationToken ct = default)
    {
        var user = new User { FirstName = firstName.Trim(), LastName = lastName.Trim(), Email = email.Trim().ToLowerInvariant(), Role = UserRole.Student, IsActive = true, IsEmailVerified = true };
        return await CreateUserAsync(user, passwordHash, ct);
    }

    public async Task<Result<User>> CreateUserAsync(User user, string passwordHash, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName)) return Result<User>.Failure("First name and last name are required.");
        if (string.IsNullOrWhiteSpace(user.Email)) return Result<User>.Failure("Email is required.");
        var normalizedEmail = user.Email.Trim().ToLowerInvariant();
        if (await _uow.Users.EmailExistsAsync(normalizedEmail, null, ct)) return Result<User>.Failure("An account with that email already exists.");
        user.FirstName = user.FirstName.Trim();
        user.LastName = user.LastName.Trim();
        user.Email = normalizedEmail;
        user.PasswordHash = passwordHash;
        user.IsActive = true;
        user.IsEmailVerified = true;
        await _uow.Users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);
        return Result<User>.Success(user);
    }

    public async Task<Result<User>> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(id, ct);
        if (user == null) return Result<User>.Failure("User not found.");
        return Result<User>.Success(user);
    }

    public async Task<Result<User>> GetUserByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByEmailAsync(email, ct);
        if (user == null) return Result<User>.Failure("User not found.");
        return Result<User>.Success(user);
    }

    public async Task<Result<IEnumerable<User>>> GetAllUsersAsync(CancellationToken ct = default)
    {
        var users = await _uow.Users.GetAllAsync(ct);
        return Result<IEnumerable<User>>.Success(users.OrderBy(u => u.Role).ThenBy(u => u.FirstName).ThenBy(u => u.LastName));
    }

    public async Task<Result<IEnumerable<User>>> GetAllInstructorsAsync(CancellationToken ct = default)
    {
        var instructors = await _uow.Users.GetInstructorsAsync(ct);
        return Result<IEnumerable<User>>.Success(instructors);
    }

    public async Task<Result> UpdateProfileAsync(User user, CancellationToken ct = default)
    {
        if (await _uow.Users.EmailExistsAsync(user.Email, user.Id, ct)) return Result.Failure("Email is already in use by another account.");
        user.Email = user.Email.Trim().ToLowerInvariant();
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> SetActiveStatusAsync(Guid id, bool isActive, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(id, ct);
        if (user == null) return Result.Failure("User not found.");
        user.IsActive = isActive;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> SetRoleAsync(Guid id, UserRole role, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(id, ct);
        if (user == null) return Result.Failure("User not found.");
        user.Role = role;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(Guid id, string passwordHash, CancellationToken ct = default)
    {
        var user = await _uow.Users.GetByIdAsync(id, ct);
        if (user == null) return Result.Failure("User not found.");
        user.PasswordHash = passwordHash;
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
