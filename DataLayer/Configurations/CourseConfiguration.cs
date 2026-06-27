using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Slug).IsRequired().HasMaxLength(220);
        builder.HasIndex(c => c.Slug).IsUnique().HasDatabaseName("IX_Courses_Slug");
        builder.Property(c => c.ShortDescription).IsRequired().HasMaxLength(500);
        builder.Property(c => c.Description).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(c => c.ThumbnailUrl).HasMaxLength(2048);
        builder.Property(c => c.PreviewVideoUrl).HasMaxLength(2048);
        builder.Property(c => c.Price).IsRequired().HasColumnType("decimal(18,2)");
        builder.Property(c => c.DiscountedPrice).HasColumnType("decimal(18,2)");
        builder.Property(c => c.Level).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Language).IsRequired().HasMaxLength(50);
        builder.Property(c => c.WhatYouWillLearn).HasColumnType("nvarchar(max)");
        builder.Property(c => c.Requirements).HasColumnType("nvarchar(max)");
        builder.Property(c => c.TargetAudience).HasColumnType("nvarchar(max)");
        builder.Property(c => c.AverageRating).HasColumnType("decimal(3,2)");

        builder.HasIndex(c => c.Status).HasDatabaseName("IX_Courses_Status");
        builder.HasIndex(c => c.CategoryId).HasDatabaseName("IX_Courses_CategoryId");
        builder.HasIndex(c => c.InstructorId).HasDatabaseName("IX_Courses_InstructorId");
        builder.HasIndex(c => c.IsFeatured).HasDatabaseName("IX_Courses_IsFeatured");

        builder.HasOne(c => c.Instructor)
            .WithMany(u => u.CoursesCreated)
            .HasForeignKey(c => c.InstructorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Category)
            .WithMany(cat => cat.Courses)
            .HasForeignKey(c => c.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("Courses");
    }
}
