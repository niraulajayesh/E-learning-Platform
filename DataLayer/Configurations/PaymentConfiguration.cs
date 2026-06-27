using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.OriginalAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.DiscountAmount)
            .IsRequired()
            .HasDefaultValue(0m)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("USD");

        builder.Property(p => p.GatewayName)
            .HasMaxLength(50);

        builder.Property(p => p.GatewayReference)
            .HasMaxLength(256);

        builder.Property(p => p.GatewayResponse)
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(p => p.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(p => p.StudentId).HasDatabaseName("IX_Payments_StudentId");
        builder.HasIndex(p => p.CourseId).HasDatabaseName("IX_Payments_CourseId");
        builder.HasIndex(p => p.Status).HasDatabaseName("IX_Payments_Status");
        builder.HasIndex(p => p.GatewayReference).HasDatabaseName("IX_Payments_GatewayReference");

        // Relationship: Payment → Student
        builder.HasOne(p => p.Student)
            .WithMany(u => u.Payments)
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship: Payment → Course
        builder.HasOne(p => p.Course)
            .WithMany(c => c.Payments)
            .HasForeignKey(p => p.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship: Payment → Coupon (optional)
        builder.HasOne(p => p.Coupon)
            .WithMany(c => c.Payments)
            .HasForeignKey(p => p.CouponId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.ToTable("Payments");
    }
}
