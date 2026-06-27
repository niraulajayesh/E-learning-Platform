using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class StudyGuideConfiguration : IEntityTypeConfiguration<StudyGuide>
{
    public void Configure(EntityTypeBuilder<StudyGuide> builder)
    {
        builder.HasKey(g => g.Id);
        builder.Property(g => g.Title).IsRequired().HasMaxLength(180);
        builder.Property(g => g.Summary).HasMaxLength(800);
        builder.Property(g => g.Theory).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(g => g.Examples).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(g => g.KeyConcepts).IsRequired().HasColumnType("nvarchar(max)");
        builder.Property(g => g.Tips).IsRequired().HasColumnType("nvarchar(max)");
        builder.HasIndex(g => new { g.CategoryId, g.DisplayOrder }).HasDatabaseName("IX_StudyGuides_Category_Order");
        builder.HasOne(g => g.Category).WithMany().HasForeignKey(g => g.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable("StudyGuides");
    }
}
