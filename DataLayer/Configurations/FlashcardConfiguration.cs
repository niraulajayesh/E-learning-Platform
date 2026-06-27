using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class FlashcardConfiguration : IEntityTypeConfiguration<Flashcard>
{
    public void Configure(EntityTypeBuilder<Flashcard> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Front).IsRequired().HasMaxLength(500);
        builder.Property(c => c.Back).IsRequired().HasMaxLength(1000);
        builder.Property(c => c.Hint).HasMaxLength(500);
        builder.HasIndex(c => new { c.FlashcardSetId, c.Order }).HasDatabaseName("IX_Flashcards_Set_Order");
        builder.HasOne(c => c.FlashcardSet).WithMany(s => s.Flashcards).HasForeignKey(c => c.FlashcardSetId).OnDelete(DeleteBehavior.Cascade);
        builder.ToTable("Flashcards");
    }
}
