using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class FlashcardSetConfiguration : IEntityTypeConfiguration<FlashcardSet>
{
    public void Configure(EntityTypeBuilder<FlashcardSet> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Title).IsRequired().HasMaxLength(180);
        builder.Property(s => s.Description).HasMaxLength(800);
        builder.HasIndex(s => s.CategoryId).HasDatabaseName("IX_FlashcardSets_CategoryId");
        builder.HasOne(s => s.Category).WithMany().HasForeignKey(s => s.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(s => s.Cards);
        builder.ToTable("FlashcardSets");
    }
}
