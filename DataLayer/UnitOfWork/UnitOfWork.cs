using DataLayer.Context;
using DataLayer.Repositories.Implementations;
using DataLayer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace DataLayer.UnitOfWork;

/// <summary>
/// Concrete Unit of Work that wraps AppDbContext and coordinates all repositories
/// under a single transaction boundary.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    // Lazy-initialised repositories — only created when first accessed
    private ICourseRepository?       _courses;
    private ICategoryRepository?     _categories;
    private IUserRepository?         _users;
    private IEnrollmentRepository?   _enrollments;
    private IProgressRepository?     _progress;
    private ILessonRepository?       _lessons;
    private IQuizRepository?         _quizzes;
    private IReviewRepository?       _reviews;
    private IPaymentRepository?      _payments;
    private ICertificateRepository?  _certificates;
    private ICouponRepository?       _coupons;
    private INotificationRepository? _notifications;
    private ITestimonialRepository?  _testimonials;
    private IBannerRepository?       _banners;
    private ISiteSettingRepository?  _siteSettings;
    private IWishlistRepository?     _wishlists;

    public UnitOfWork(AppDbContext context) => _context = context;

    // ── Repository Properties ──────────────────────────────────────────────

    public ICourseRepository      Courses        => _courses       ??= new CourseRepository(_context);
    public ICategoryRepository    Categories     => _categories    ??= new CategoryRepository(_context);
    public IUserRepository        Users          => _users         ??= new UserRepository(_context);
    public IEnrollmentRepository  Enrollments    => _enrollments   ??= new EnrollmentRepository(_context);
    public IProgressRepository    Progress       => _progress      ??= new ProgressRepository(_context);
    public ILessonRepository      Lessons        => _lessons       ??= new LessonRepository(_context);
    public IQuizRepository        Quizzes        => _quizzes       ??= new QuizRepository(_context);
    public IReviewRepository      Reviews        => _reviews       ??= new ReviewRepository(_context);
    public IPaymentRepository     Payments       => _payments      ??= new PaymentRepository(_context);
    public ICertificateRepository Certificates   => _certificates  ??= new CertificateRepository(_context);
    public ICouponRepository      Coupons        => _coupons       ??= new CouponRepository(_context);
    public INotificationRepository Notifications => _notifications ??= new NotificationRepository(_context);
    public ITestimonialRepository Testimonials   => _testimonials  ??= new TestimonialRepository(_context);
    public IBannerRepository      Banners        => _banners       ??= new BannerRepository(_context);
    public ISiteSettingRepository SiteSettings   => _siteSettings  ??= new SiteSettingRepository(_context);
    public IWishlistRepository    Wishlists      => _wishlists     ??= new WishlistRepository(_context);

    // ── Transaction Management ─────────────────────────────────────────────

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _transaction = await _context.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        await _transaction.CommitAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;

        await _transaction.RollbackAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    // ── Disposal ───────────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
            await _transaction.DisposeAsync();

        await _context.DisposeAsync();
    }
}
