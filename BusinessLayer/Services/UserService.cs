using BusinessLayer.Interfaces;
using DataLayer.Context;
using DataLayer.Entities;
using DataLayer.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using SharedLayer.Enums;
using SharedLayer.Wrappers;

namespace BusinessLayer.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _db;

    public UserService(IUnitOfWork uow, AppDbContext db)
    {
        _uow = uow;
        _db = db;
    }

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
    public async Task<Result> DeleteUserAsync(Guid id, string? currentUserEmail = null, CancellationToken ct = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user == null) return Result.Failure("User not found.");

        if (IsProtectedPlatformAdmin(user))
            return Result.Failure("The Platform Admin account cannot be deleted.");

        if (!string.IsNullOrWhiteSpace(currentUserEmail) && user.Email.Equals(currentUserEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            return Result.Failure("You cannot delete the currently logged-in administrator.");

        if (user.Role == UserRole.Admin)
        {
            var remainingAdmins = await _db.Users.CountAsync(u => u.Role == UserRole.Admin && u.Id != id, ct);
            if (remainingAdmins == 0) return Result.Failure("You cannot delete the last remaining administrator.");
        }


        var fallbackAdminId = await _db.Users
            .Where(u => u.Role == UserRole.Admin && u.Id != id)
            .OrderBy(u => u.CreatedAt)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(ct);

        var authoredCourses = await _db.Courses.Where(c => c.InstructorId == id).ToListAsync(ct);
        if (authoredCourses.Count > 0)
        {
            if (fallbackAdminId == Guid.Empty)
                return Result.Failure("This instructor owns courses and no administrator is available to receive them.");

            foreach (var course in authoredCourses)
            {
                course.InstructorId = fallbackAdminId;
            }
        }

        await RemoveStudentDataAsync(id, ct);

        var auditLogs = await _db.AuditLogs.Where(a => a.UserId == id).ToListAsync(ct);
        foreach (var auditLog in auditLogs)
        {
            auditLog.UserId = null;
        }

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task RemoveStudentDataAsync(Guid userId, CancellationToken ct)
    {
        _db.StudyGuideBookmarks.RemoveRange(await _db.StudyGuideBookmarks.Where(b => b.StudentId == userId).ToListAsync(ct));
        _db.Wishlists.RemoveRange(await _db.Wishlists.Where(w => w.StudentId == userId).ToListAsync(ct));
        _db.StudyPlanners.RemoveRange(await _db.StudyPlanners.Where(p => p.StudentId == userId).ToListAsync(ct));
        _db.PracticeTestAttempts.RemoveRange(await _db.PracticeTestAttempts.Where(a => a.StudentId == userId).ToListAsync(ct));
        _db.FullExamAttempts.RemoveRange(await _db.FullExamAttempts.Where(a => a.StudentId == userId).ToListAsync(ct));
        _db.QuizAttempts.RemoveRange(await _db.QuizAttempts.Where(a => a.StudentId == userId).ToListAsync(ct));
        _db.Certificates.RemoveRange(await _db.Certificates.Where(c => c.StudentId == userId).ToListAsync(ct));
        _db.Payments.RemoveRange(await _db.Payments.Where(p => p.StudentId == userId).ToListAsync(ct));
        _db.Reviews.RemoveRange(await _db.Reviews.Where(r => r.StudentId == userId).ToListAsync(ct));
        _db.Notifications.RemoveRange(await _db.Notifications.Where(n => n.UserId == userId).ToListAsync(ct));

        var enrollmentIds = await _db.Enrollments.Where(e => e.StudentId == userId).Select(e => e.Id).ToListAsync(ct);
        if (enrollmentIds.Count > 0)
        {
            _db.Progress.RemoveRange(await _db.Progress.Where(p => enrollmentIds.Contains(p.EnrollmentId)).ToListAsync(ct));
            _db.Enrollments.RemoveRange(await _db.Enrollments.Where(e => enrollmentIds.Contains(e.Id)).ToListAsync(ct));
        }
    }

    private static bool IsProtectedPlatformAdmin(User user)
    {
        return user.Role == UserRole.Admin
            && (user.Email.Equals("admin@learnhub.local", StringComparison.OrdinalIgnoreCase)
                || user.FullName.Equals("Platform Admin", StringComparison.OrdinalIgnoreCase));
    }
}
