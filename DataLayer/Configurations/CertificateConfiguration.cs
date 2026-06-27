using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CertificateNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(c => c.CertificateNumber)
            .IsUnique()
            .HasDatabaseName("IX_Certificates_CertificateNumber");

        builder.Property(c => c.PdfUrl)
            .HasMaxLength(2048);

        builder.Property(c => c.VerificationUrl)
            .HasMaxLength(2048);

        builder.Property(c => c.IssuedAt)
            .IsRequired();

        // One certificate per enrollment (1-to-1)
        builder.HasIndex(c => c.EnrollmentId)
            .IsUnique()
            .HasDatabaseName("IX_Certificates_EnrollmentId");

        builder.HasIndex(c => c.StudentId)
            .HasDatabaseName("IX_Certificates_StudentId");

        // Relationship: Certificate → Enrollment (1-to-1)
        builder.HasOne(c => c.Enrollment)
            .WithOne(e => e.Certificate)
            .HasForeignKey<Certificate>(c => c.EnrollmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship: Certificate → Student
        builder.HasOne(c => c.Student)
            .WithMany(u => u.Certificates)
            .HasForeignKey(c => c.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relationship: Certificate → Course
        builder.HasOne(c => c.Course)
            .WithMany(co => co.Certificates)
            .HasForeignKey(c => c.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("Certificates");
    }
}
