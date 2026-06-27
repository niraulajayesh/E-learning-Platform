using BusinessLayer.Interfaces;
using BusinessLayer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessLayer.Extensions;

public static class BusinessLayerServiceExtensions
{
    public static IServiceCollection AddBusinessLayer(this IServiceCollection services)
    {
        services.AddScoped<ICourseService, CourseService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ILessonService, LessonService>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IProgressService, ProgressService>();
        services.AddScoped<IQuizService, QuizService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<ICertificateService, CertificateService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ICmsService, CmsService>();
        services.AddScoped<IWishlistService, WishlistService>();

        return services;
    }
}
