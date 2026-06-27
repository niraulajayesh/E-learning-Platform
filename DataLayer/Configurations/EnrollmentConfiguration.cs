using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(e => e.CompletionPercentage)
            .IsRequired()
            .HasDefaultValue(0.0)
            .HasColumnType("decimal(5,2)");

        builder.Property(e => e.EnrolledAt)
            .IsRequired();

        // Enforce one active enrollment per (Student, Course)
        builder.HasIndex(e => new { e.StudentId, e.CourseId })
            .IsUnique()
            .HasDatabaseName("IX_Enrollments_StudentId_CourseId");

        builder.HasIndex(e => e.StudentId).HasDatabaseName("IX_Enrollments_StudentId");
        builder.HasIndex(e => e.CourseId).HasDatabaseName("IX_Enrollments_CourseId");

        // Relationship: Enrollment → Student
        builder.HasOne(e => e.Student)
            .WithMany(u => u.Enrollments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship: Enrollment → Course
        builder.HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("Enrollments");
    }
}
