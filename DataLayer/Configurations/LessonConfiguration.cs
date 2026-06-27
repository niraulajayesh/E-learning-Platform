using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Title).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Description).HasMaxLength(1000);
        builder.Property(l => l.Type).IsRequired().HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.ContentUrl).HasMaxLength(2048);
        builder.Property(l => l.ArticleContent).HasColumnType("nvarchar(max)");
        builder.Property(l => l.ResourcesUrl).HasMaxLength(2048);
        builder.Property(l => l.DurationMinutes).IsRequired().HasDefaultValue(0);
        builder.Property(l => l.Order).IsRequired().HasDefaultValue(0);

        builder.HasIndex(l => new { l.SectionId, l.Order }).HasDatabaseName("IX_Lessons_SectionId_Order");

        builder.HasOne(l => l.Section)
            .WithMany(s => s.Lessons)
            .HasForeignKey(l => l.SectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Lessons");
    }
}
