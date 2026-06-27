using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class WishlistConfiguration : IEntityTypeConfiguration<Wishlist>
{
    public void Configure(EntityTypeBuilder<Wishlist> builder)
    {
        builder.HasOne(w => w.Student)
            .WithMany()
            .HasForeignKey(w => w.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Course)
            .WithMany()
            .HasForeignKey(w => w.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // A user can only wishlist a specific course once
        builder.HasIndex(w => new { w.StudentId, w.CourseId }).IsUnique();
    }
}
