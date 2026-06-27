using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
    public void Configure(EntityTypeBuilder<Quiz> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(q => q.Description)
            .HasMaxLength(1000);

        builder.Property(q => q.PassingScore)
            .IsRequired()
            .HasDefaultValue(70);

        builder.Property(q => q.MaxAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(q => q.IsPublished)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(q => q.ShuffleAnswers)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(q => q.LessonId)
            .IsUnique()
            .HasDatabaseName("IX_Quizzes_LessonId");

        builder.HasIndex(q => q.IsPublished)
            .HasDatabaseName("IX_Quizzes_IsPublished");

        builder.HasOne(q => q.Lesson)
            .WithOne(l => l.Quiz)
            .HasForeignKey<Quiz>(q => q.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Quizzes");
    }
}
