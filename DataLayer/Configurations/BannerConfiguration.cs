using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class BannerConfiguration : IEntityTypeConfiguration<Banner>
{
    public void Configure(EntityTypeBuilder<Banner> builder)
    {
        builder.Property(b => b.Title).HasMaxLength(200).IsRequired();
        builder.Property(b => b.Subtitle).HasMaxLength(500);
        builder.Property(b => b.ImageUrl).HasMaxLength(500).IsRequired();
        builder.Property(b => b.ButtonText).HasMaxLength(50);
        builder.Property(b => b.ButtonLink).HasMaxLength(500);
    }
}
