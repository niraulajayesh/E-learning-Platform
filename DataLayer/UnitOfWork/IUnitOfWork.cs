using DataLayer.Repositories.Interfaces;

namespace DataLayer.UnitOfWork;

/// <summary>
/// Aggregates all repositories under a single transaction scope.
/// Callers work through IUnitOfWork so the entire operation succeeds or rolls back atomically.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    // ── Repositories ───────────────────────────────────────────────────────
    ICourseRepository      Courses       { get; }
    ICategoryRepository    Categories    { get; }
    IUserRepository        Users         { get; }
    IEnrollmentRepository  Enrollments   { get; }
    IProgressRepository    Progress      { get; }
    ILessonRepository      Lessons       { get; }
    IQuizRepository        Quizzes       { get; }
    IReviewRepository      Reviews       { get; }
    IPaymentRepository     Payments      { get; }
    ICertificateRepository Certificates  { get; }
    ICouponRepository      Coupons       { get; }
    INotificationRepository Notifications { get; }
    ITestimonialRepository Testimonials  { get; }
    IBannerRepository      Banners       { get; }
    ISiteSettingRepository SiteSettings  { get; }
    IWishlistRepository    Wishlists     { get; }

    // ── Transaction ────────────────────────────────────────────────────────

    /// <summary>Persists all tracked changes in a single database transaction.</summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Begins an explicit database transaction for multi-step operations.</summary>
    Task BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>Commits the current explicit transaction.</summary>
    Task CommitTransactionAsync(CancellationToken ct = default);

    /// <summary>Rolls back the current explicit transaction.</summary>
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
