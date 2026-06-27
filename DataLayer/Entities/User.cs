using SharedLayer.Enums;

namespace DataLayer.Entities;

/// <summary>
/// Represents a registered platform user (Student, Instructor, or Admin).
/// </summary>
public class User : BaseGuidEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Student;
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    public string? Headline { get; set; }
    public string? WebsiteUrl { get; set; }
    public string? LinkedInUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public ICollection<Course> CoursesCreated { get; set; } = new List<Course>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<QuizAttempt> QuizAttempts { get; set; } = new List<QuizAttempt>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();

    // Computed
    public string FullName => $"{FirstName} {LastName}";
}
