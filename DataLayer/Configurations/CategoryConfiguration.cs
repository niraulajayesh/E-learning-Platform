using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(c => c.Slug)
            .IsUnique()
            .HasDatabaseName("IX_Categories_Slug");

        builder.Property(c => c.Description)
            .HasMaxLength(1000);

        builder.Property(c => c.IconUrl)
            .HasMaxLength(2048);

        builder.Property(c => c.BannerUrl)
            .HasMaxLength(2048);

        // Self-referencing relationship: sub-categories
        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.ToTable("Categories");
    }
}
