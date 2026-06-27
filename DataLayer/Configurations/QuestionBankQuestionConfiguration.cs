using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class QuestionBankQuestionConfiguration : IEntityTypeConfiguration<QuestionBankQuestion>
{
    public void Configure(EntityTypeBuilder<QuestionBankQuestion> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Topic).HasMaxLength(120);
        builder.Property(q => q.Subtopic).HasMaxLength(160);
        builder.Property(q => q.Difficulty).IsRequired().HasMaxLength(20);
        builder.Property(q => q.Text).IsRequired().HasMaxLength(1200);
        builder.Property(q => q.QuestionImageUrl).HasMaxLength(500);
        builder.Property(q => q.ExplanationImageUrl).HasMaxLength(500);
        builder.Property(q => q.OptionA).IsRequired().HasMaxLength(500);
        builder.Property(q => q.OptionB).IsRequired().HasMaxLength(500);
        builder.Property(q => q.OptionC).IsRequired().HasMaxLength(500);
        builder.Property(q => q.OptionD).IsRequired().HasMaxLength(500);
        builder.Property(q => q.OptionE).HasMaxLength(500);
        builder.Property(q => q.OptionF).HasMaxLength(500);
        builder.Property(q => q.CorrectOption).IsRequired().HasMaxLength(1);
        builder.Property(q => q.Explanation).IsRequired().HasMaxLength(2000);
        builder.Property(q => q.WrongAnswerExplanation).IsRequired().HasMaxLength(3000);
        builder.Property(q => q.SourceReference).HasMaxLength(300);
        builder.Property(q => q.Tags).HasMaxLength(500);
        builder.Property(q => q.EstimatedTimeSeconds).HasDefaultValue(60);
        builder.Property(q => q.Status).IsRequired().HasMaxLength(20).HasDefaultValue("Draft");
        builder.Property(q => q.CreatedBy).HasMaxLength(120);
        builder.Property(q => q.ModifiedBy).HasMaxLength(120);
        builder.HasIndex(q => new { q.CategoryId, q.Difficulty }).HasDatabaseName("IX_QuestionBank_Category_Difficulty");
        builder.HasIndex(q => q.Status).HasDatabaseName("IX_QuestionBank_Status");
        builder.HasIndex(q => q.Topic).HasDatabaseName("IX_QuestionBank_Topic");
        builder.HasOne(q => q.Category).WithMany().HasForeignKey(q => q.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable("QuestionBankQuestions");
    }
}
