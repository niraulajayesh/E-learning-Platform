using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class TestimonialConfiguration : IEntityTypeConfiguration<Testimonial>
{
    public void Configure(EntityTypeBuilder<Testimonial> builder)
    {
        builder.Property(t => t.StudentName).HasMaxLength(100).IsRequired();
        builder.Property(t => t.StudentRole).HasMaxLength(100);
        builder.Property(t => t.ProfilePictureUrl).HasMaxLength(500);
        builder.Property(t => t.Content).HasMaxLength(1000).IsRequired();

        builder.ToTable(t => t.HasCheckConstraint("CK_Testimonial_Rating", "Rating >= 1 AND Rating <= 5"));
    }
}
