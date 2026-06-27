using DataLayer.Configurations;
using DataLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Context;

/// <summary>
/// Main EF Core DbContext for the E-Learning Platform.
/// All entity configurations are applied via IEntityTypeConfiguration classes
/// discovered automatically from this assembly.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ─── DbSets ────────────────────────────────────────────────────────────

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<Progress> Progress => Set<Progress>();
    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Answer> Answers => Set<Answer>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Payment> Payments { get; set; }
    public DbSet<Certificate> Certificates { get; set; }
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<Testimonial> Testimonials { get; set; }
    public DbSet<Banner> Banners { get; set; }
    public DbSet<SiteSetting> SiteSettings { get; set; }
    public DbSet<Wishlist> Wishlists { get; set; }
    public DbSet<FlashcardSet> FlashcardSets { get; set; }
    public DbSet<Flashcard> Flashcards { get; set; }
    public DbSet<StudyGuide> StudyGuides { get; set; }
    public DbSet<StudyGuideBookmark> StudyGuideBookmarks { get; set; }
    public DbSet<PracticeTest> PracticeTests { get; set; }
    public DbSet<PracticeTestQuestion> PracticeTestQuestions { get; set; }
    public DbSet<PracticeTestAttempt> PracticeTestAttempts { get; set; }
    public DbSet<QuestionBankQuestion> QuestionBankQuestions { get; set; }
    public DbSet<FullExamAttempt> FullExamAttempts { get; set; }
    public DbSet<StudyPlanner> StudyPlanners { get; set; }

    // ─── Model Configuration ───────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> classes in this assembly automatically
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    // ─── Auto-update Timestamps ────────────────────────────────────────────

    /// <summary>
    /// Automatically sets CreatedAt and UpdatedAt before every save.
    /// </summary>
    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    // Prevent overwriting the original CreatedAt on updates
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    break;
            }
        }
    }
}






