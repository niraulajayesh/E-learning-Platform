using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class PracticeTestQuestionConfiguration : IEntityTypeConfiguration<PracticeTestQuestion>
{
    public void Configure(EntityTypeBuilder<PracticeTestQuestion> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Text).IsRequired().HasMaxLength(1000);
        builder.Property(q => q.OptionA).IsRequired().HasMaxLength(500);
        builder.Property(q => q.OptionB).IsRequired().HasMaxLength(500);
        builder.Property(q => q.OptionC).IsRequired().HasMaxLength(500);
        builder.Property(q => q.OptionD).IsRequired().HasMaxLength(500);
        builder.Property(q => q.OptionE).HasMaxLength(500);
        builder.Property(q => q.OptionF).HasMaxLength(500);
        builder.Property(q => q.CorrectOption).IsRequired().HasMaxLength(1);
        builder.Property(q => q.Explanation).HasMaxLength(2000);
        builder.HasIndex(q => new { q.PracticeTestId, q.Order }).HasDatabaseName("IX_PracticeTestQuestions_Test_Order");
        builder.HasIndex(q => q.QuestionBankQuestionId).HasDatabaseName("IX_PracticeTestQuestions_QuestionBankQuestionId");
        builder.HasOne(q => q.PracticeTest).WithMany(t => t.Questions).HasForeignKey(q => q.PracticeTestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(q => q.QuestionBankQuestion).WithMany(q => q.PracticeTestQuestions).HasForeignKey(q => q.QuestionBankQuestionId).OnDelete(DeleteBehavior.SetNull);
        builder.ToTable("PracticeTestQuestions");
    }
}
