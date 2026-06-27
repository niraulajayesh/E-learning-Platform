using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(n => n.ActionUrl)
            .HasMaxLength(2048);

        builder.Property(n => n.IconClass)
            .HasMaxLength(100);

        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        // Index for unread notification queries per user
        builder.HasIndex(n => new { n.UserId, n.IsRead })
            .HasDatabaseName("IX_Notifications_UserId_IsRead");

        builder.HasIndex(n => n.CreatedAt)
            .HasDatabaseName("IX_Notifications_CreatedAt");

        // Relationship: Notification → User
        builder.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("Notifications");
    }
}
