using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Text)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(q => q.ImageUrl)
            .HasMaxLength(500);

        builder.Property(q => q.Explanation)
            .HasMaxLength(2000);

        builder.Property(q => q.Difficulty)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("Easy");

        builder.Property(q => q.Type)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(q => q.Points)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(q => q.Order)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(q => new { q.QuizId, q.Order })
            .HasDatabaseName("IX_Questions_QuizId_Order");

        builder.HasIndex(q => q.QuestionBankQuestionId)
            .HasDatabaseName("IX_Questions_QuestionBankQuestionId");

        builder.HasIndex(q => q.CategoryId)
            .HasDatabaseName("IX_Questions_CategoryId");

        builder.HasOne(q => q.Quiz)
            .WithMany(qz => qz.Questions)
            .HasForeignKey(q => q.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(q => q.Category)
            .WithMany()
            .HasForeignKey(q => q.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(q => q.QuestionBankQuestion)
            .WithMany()
            .HasForeignKey(q => q.QuestionBankQuestionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable("Questions");
    }
}
