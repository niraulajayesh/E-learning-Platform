using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> builder)
    {
        builder.HasKey(qa => qa.Id);

        builder.Property(qa => qa.Score)
            .IsRequired()
            .HasDefaultValue(0.0)
            .HasColumnType("decimal(5,2)");

        builder.Property(qa => qa.TotalPoints)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(qa => qa.EarnedPoints)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(qa => qa.AttemptNumber)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(qa => qa.TimeTakenSeconds)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(qa => qa.AnswersSnapshot)
            .HasColumnType("nvarchar(max)");

        builder.Property(qa => qa.AttemptedAt)
            .IsRequired();

        builder.HasIndex(qa => new { qa.QuizId, qa.StudentId })
            .HasDatabaseName("IX_QuizAttempts_QuizId_StudentId");

        builder.HasIndex(qa => qa.StudentId)
            .HasDatabaseName("IX_QuizAttempts_StudentId");

        // Relationship: QuizAttempt → Quiz
        builder.HasOne(qa => qa.Quiz)
            .WithMany(q => q.Attempts)
            .HasForeignKey(qa => qa.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship: QuizAttempt → Student
        builder.HasOne(qa => qa.Student)
            .WithMany(u => u.QuizAttempts)
            .HasForeignKey(qa => qa.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("QuizAttempts");
    }
}
