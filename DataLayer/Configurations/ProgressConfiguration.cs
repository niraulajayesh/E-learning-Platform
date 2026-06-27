using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class ProgressConfiguration : IEntityTypeConfiguration<Progress>
{
    public void Configure(EntityTypeBuilder<Progress> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.WatchedSeconds)
            .IsRequired()
            .HasDefaultValue(0);

        // One progress record per (Enrollment, Lesson)
        builder.HasIndex(p => new { p.EnrollmentId, p.LessonId })
            .IsUnique()
            .HasDatabaseName("IX_Progress_EnrollmentId_LessonId");

        builder.HasIndex(p => p.EnrollmentId)
            .HasDatabaseName("IX_Progress_EnrollmentId");

        // Relationship: Progress → Enrollment
        builder.HasOne(p => p.Enrollment)
            .WithMany(e => e.ProgressRecords)
            .HasForeignKey(p => p.EnrollmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship: Progress → Lesson
        builder.HasOne(p => p.Lesson)
            .WithMany(l => l.ProgressRecords)
            .HasForeignKey(p => p.LessonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("Progress");
    }
}
