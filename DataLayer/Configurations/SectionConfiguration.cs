using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Description)
            .HasMaxLength(500);

        builder.Property(s => s.Order)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(s => new { s.CourseId, s.Order })
            .HasDatabaseName("IX_Sections_CourseId_Order");

        // Relationship: Section → Course
        builder.HasOne(s => s.Course)
            .WithMany(c => c.Sections)
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Sections");
    }
}
