using DataLayer.Context;
using DataLayer.Repositories.Implementations;
using DataLayer.Repositories.Interfaces;
using DataLayer.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DataLayer.Extensions;

/// <summary>
/// Registers all DataLayer services into the DI container.
/// Call this once from Program.cs in UserLayer and AdminLayer.
/// </summary>
public static class DataLayerServiceExtensions
{
    public static IServiceCollection AddDataLayer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── AppDbContext ───────────────────────────────────────────────────
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                    sqlOptions.CommandTimeout(60);
                    sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                });

            if (string.Equals(configuration["Data:EnableSensitiveLogging"], "true", StringComparison.OrdinalIgnoreCase))
            {
                options.EnableSensitiveDataLogging();
            }

            if (string.Equals(configuration["Data:EnableDetailedErrors"], "true", StringComparison.OrdinalIgnoreCase))
            {
                options.EnableDetailedErrors();
            }
        });

        // ── Unit of Work ──────────────────────────────────────────────────
        services.AddScoped<IUnitOfWork, DataLayer.UnitOfWork.UnitOfWork>();

        // ── Domain Repositories ────────────────────────────────────────────
        services.AddScoped<ICourseRepository,       CourseRepository>();
        services.AddScoped<ICategoryRepository,     CategoryRepository>();
        services.AddScoped<IUserRepository,         UserRepository>();
        services.AddScoped<IEnrollmentRepository,   EnrollmentRepository>();
        services.AddScoped<IProgressRepository,     ProgressRepository>();
        services.AddScoped<ILessonRepository,       LessonRepository>();
        services.AddScoped<IQuizRepository,         QuizRepository>();
        services.AddScoped<IReviewRepository,       ReviewRepository>();
        services.AddScoped<IPaymentRepository,      PaymentRepository>();
        services.AddScoped<ICertificateRepository,  CertificateRepository>();
        services.AddScoped<ICouponRepository,       CouponRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<ITestimonialRepository,  TestimonialRepository>();
        services.AddScoped<IBannerRepository,       BannerRepository>();
        services.AddScoped<ISiteSettingRepository,  SiteSettingRepository>();
        services.AddScoped<IWishlistRepository,     WishlistRepository>();

        return services;
    }

    /// <summary>
    /// Applies any pending EF Core migrations on startup.
    /// Call this in development environments only.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }
}

