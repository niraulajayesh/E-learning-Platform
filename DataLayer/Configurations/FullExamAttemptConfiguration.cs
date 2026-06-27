using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class FullExamAttemptConfiguration : IEntityTypeConfiguration<FullExamAttempt>
{
    public void Configure(EntityTypeBuilder<FullExamAttempt> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.ScorePercent).HasColumnType("decimal(5,2)");
        builder.Property(e => e.CategoryBreakdownJson).HasColumnType("nvarchar(max)");
        builder.Property(e => e.AnswersJson).HasColumnType("nvarchar(max)");
        builder.Property(e => e.SummaryReport).HasColumnType("nvarchar(max)");
        builder.HasIndex(e => e.StudentId).HasDatabaseName("IX_FullExamAttempts_StudentId");
        builder.HasOne(e => e.Student).WithMany().HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.ToTable("FullExamAttempts");
    }
}
