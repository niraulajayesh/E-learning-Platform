using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code)
            .IsRequired()
            .HasMaxLength(50);

        // Case-insensitive unique index on coupon code
        builder.HasIndex(c => c.Code)
            .IsUnique()
            .HasDatabaseName("IX_Coupons_Code");

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.DiscountPercentage)
            .IsRequired();

        builder.ToTable("Coupons", t =>
            t.HasCheckConstraint("CK_Coupons_DiscountPercentage",
                "[DiscountPercentage] >= 1 AND [DiscountPercentage] <= 100"));

        builder.Property(c => c.MaxDiscountAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(c => c.MaxUses)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(c => c.UsedCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(c => c.IsActive)
            .HasDatabaseName("IX_Coupons_IsActive");
    }
}
