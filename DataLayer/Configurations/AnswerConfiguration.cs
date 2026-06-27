using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class AnswerConfiguration : IEntityTypeConfiguration<Answer>
{
    public void Configure(EntityTypeBuilder<Answer> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Text)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.IsCorrect)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.Order)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(a => new { a.QuestionId, a.Order })
            .HasDatabaseName("IX_Answers_QuestionId_Order");

        // Relationship: Answer → Question
        builder.HasOne(a => a.Question)
            .WithMany(q => q.Answers)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Answers");
    }
}
