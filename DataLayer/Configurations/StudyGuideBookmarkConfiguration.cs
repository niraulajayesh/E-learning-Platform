using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class StudyGuideBookmarkConfiguration : IEntityTypeConfiguration<StudyGuideBookmark>
{
    public void Configure(EntityTypeBuilder<StudyGuideBookmark> builder)
    {
        builder.HasKey(b => b.Id);
        builder.HasIndex(b => new { b.StudyGuideId, b.StudentId }).IsUnique().HasDatabaseName("IX_StudyGuideBookmarks_Guide_Student");
        builder.HasOne(b => b.StudyGuide).WithMany(g => g.Bookmarks).HasForeignKey(b => b.StudyGuideId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(b => b.Student).WithMany().HasForeignKey(b => b.StudentId).OnDelete(DeleteBehavior.Restrict);
        builder.ToTable("StudyGuideBookmarks");
    }
}
