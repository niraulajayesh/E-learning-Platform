using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Rating)
            .IsRequired();

        // Enforce rating is between 1 and 5 via check constraint
        builder.ToTable("Reviews", t =>
            t.HasCheckConstraint("CK_Reviews_Rating", "[Rating] >= 1 AND [Rating] <= 5"));

        builder.Property(r => r.Comment)
            .HasMaxLength(2000);

        builder.Property(r => r.InstructorReply)
            .HasMaxLength(2000);

        // One review per (Student, Course)
        builder.HasIndex(r => new { r.StudentId, r.CourseId })
            .IsUnique()
            .HasDatabaseName("IX_Reviews_StudentId_CourseId");

        builder.HasIndex(r => r.CourseId)
            .HasDatabaseName("IX_Reviews_CourseId");

        // Relationship: Review → Course
        builder.HasOne(r => r.Course)
            .WithMany(c => c.Reviews)
            .HasForeignKey(r => r.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship: Review → Student
        builder.HasOne(r => r.Student)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
