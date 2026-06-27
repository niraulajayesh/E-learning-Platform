using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(u => u.ProfilePictureUrl)
            .HasMaxLength(2048);

        builder.Property(u => u.Bio)
            .HasMaxLength(2000);

        builder.Property(u => u.Headline)
            .HasMaxLength(200);

        builder.Property(u => u.WebsiteUrl)
            .HasMaxLength(512);

        builder.Property(u => u.LinkedInUrl)
            .HasMaxLength(512);

        builder.Property(u => u.EmailVerificationToken)
            .HasMaxLength(512);

        builder.Property(u => u.PasswordResetToken)
            .HasMaxLength(512);

        builder.Property(u => u.RefreshToken)
            .HasMaxLength(512);

        // Relationships configured on child entities to avoid duplicate declarations
        builder.ToTable("Users");
    }
}
