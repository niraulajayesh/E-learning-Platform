using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.Configurations;

public class StudyPlannerConfiguration : IEntityTypeConfiguration<StudyPlanner>
{
    public void Configure(EntityTypeBuilder<StudyPlanner> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.DailyStudyHours).HasColumnType("decimal(4,2)");
        builder.HasIndex(p => p.StudentId).IsUnique().HasDatabaseName("IX_StudyPlanners_StudentId");
        builder.HasOne(p => p.Student).WithMany().HasForeignKey(p => p.StudentId).OnDelete(DeleteBehavior.Cascade);
        builder.ToTable("StudyPlanners");
    }
}
