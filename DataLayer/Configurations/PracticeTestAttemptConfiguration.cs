using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class PracticeTestAttemptConfiguration : IEntityTypeConfiguration<PracticeTestAttempt>
{
    public void Configure(EntityTypeBuilder<PracticeTestAttempt> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ScorePercent).HasColumnType("decimal(5,2)");
        builder.Property(a => a.AnswersJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(a => a.PracticeTestId).HasDatabaseName("IX_PracticeTestAttempts_TestId");
        builder.HasOne(a => a.PracticeTest).WithMany(t => t.Attempts).HasForeignKey(a => a.PracticeTestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(a => a.Student).WithMany().HasForeignKey(a => a.StudentId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);
        builder.ToTable("PracticeTestAttempts");
    }
}
