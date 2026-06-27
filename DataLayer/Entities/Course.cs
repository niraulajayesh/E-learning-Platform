using SharedLayer.Enums;

namespace DataLayer.Entities;

/// <summary>
/// Represents an online course created by an instructor.
/// </summary>
public class Course : BaseGuidEntity
{
    public Guid InstructorId { get; set; }
    public int CategoryId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? PreviewVideoUrl { get; set; }

    public decimal Price { get; set; } = 0m;
    public decimal? DiscountedPrice { get; set; }

    public CourseLevel Level { get; set; } = CourseLevel.AllLevels;
    public CourseStatus Status { get; set; } = CourseStatus.Draft;
    public string Language { get; set; } = "English";

    // Metadata
    public string? WhatYouWillLearn { get; set; }
    public string? Requirements { get; set; }
    public string? TargetAudience { get; set; }

    // Denormalized aggregates (updated by application logic)
    public int TotalDurationMinutes { get; set; } = 0;
    public int TotalLessons { get; set; } = 0;
    public double AverageRating { get; set; } = 0.0;
    public int TotalReviews { get; set; } = 0;
    public int TotalEnrollments { get; set; } = 0;

    public bool IsFeatured { get; set; } = false;
    public bool IsBestseller { get; set; } = false;
    public DateTime? PublishedAt { get; set; }

    // Navigation properties
    public User Instructor { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<Section> Sections { get; set; } = new List<Section>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Certificate> Certificates { get; set; } = new List<Certificate>();
}
