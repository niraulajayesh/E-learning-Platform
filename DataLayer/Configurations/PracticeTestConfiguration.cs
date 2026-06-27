using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class PracticeTestConfiguration : IEntityTypeConfiguration<PracticeTest>
{
    public void Configure(EntityTypeBuilder<PracticeTest> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Title).IsRequired().HasMaxLength(180);
        builder.Property(t => t.Description).HasMaxLength(900);
        builder.Property(t => t.PassingScorePercent).HasDefaultValue(70);
        builder.HasIndex(t => new { t.CategoryId, t.DisplayOrder }).HasDatabaseName("IX_PracticeTests_Category_Order");
        builder.HasIndex(t => t.IsMockExam).HasDatabaseName("IX_PracticeTests_IsMockExam");
        builder.HasOne(t => t.Category).WithMany().HasForeignKey(t => t.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable("PracticeTests");
    }
}
