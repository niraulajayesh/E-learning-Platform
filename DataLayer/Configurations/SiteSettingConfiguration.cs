using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> builder)
    {
        builder.Property(s => s.Key).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Value).IsRequired(); // Can be long text
        builder.Property(s => s.Description).HasMaxLength(500);

        builder.HasIndex(s => s.Key).IsUnique();
    }
}
