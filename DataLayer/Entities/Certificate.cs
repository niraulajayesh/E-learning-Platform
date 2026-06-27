namespace DataLayer.Entities;

/// <summary>
/// Represents a completion certificate issued to a student upon finishing a course (100% progress).
/// One certificate per enrollment.
/// </summary>
public class Certificate : BaseGuidEntity
{
    public Guid EnrollmentId { get; set; }
    public Guid StudentId { get; set; }
    public Guid CourseId { get; set; }

    public string CertificateNumber { get; set; } = string.Empty;   // Unique verifiable code
    public string? PdfUrl { get; set; }                             // Path/URL to generated PDF
    public string? VerificationUrl { get; set; }                    // Public verification link
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Enrollment Enrollment { get; set; } = null!;
    public User Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}
