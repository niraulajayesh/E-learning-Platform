using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        // Use database-generated identity for sequential inserts
        builder.Property(a => a.Id)
            .ValueGeneratedOnAdd();

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.OldValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.NewValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45);     // IPv6 max length

        builder.Property(a => a.UserAgent)
            .HasMaxLength(512);

        builder.Property(a => a.Timestamp)
            .IsRequired();

        // Indexes for audit queries
        builder.HasIndex(a => a.Timestamp).HasDatabaseName("IX_AuditLogs_Timestamp");
        builder.HasIndex(a => a.UserId).HasDatabaseName("IX_AuditLogs_UserId");
        builder.HasIndex(a => new { a.EntityName, a.EntityId })
            .HasDatabaseName("IX_AuditLogs_EntityName_EntityId");

        // Relationship: AuditLog → User (optional)
        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.ToTable("AuditLogs");
    }
}
