using DataLayer.Entities;
using SharedLayer.Wrappers;

namespace BusinessLayer.Interfaces;

public interface ICmsService
{
    // Testimonials
    Task<Result<IEnumerable<Testimonial>>> GetAllTestimonialsAsync(CancellationToken ct = default);
    Task<Result<Testimonial>> GetTestimonialByIdAsync(int id, CancellationToken ct = default);
    Task<Result<Testimonial>> CreateTestimonialAsync(Testimonial testimonial, CancellationToken ct = default);
    Task<Result> UpdateTestimonialAsync(Testimonial testimonial, CancellationToken ct = default);
    Task<Result> DeleteTestimonialAsync(int id, CancellationToken ct = default);

    // Banners
    Task<Result<IEnumerable<Banner>>> GetAllBannersAsync(CancellationToken ct = default);
    Task<Result<Banner>> GetBannerByIdAsync(int id, CancellationToken ct = default);
    Task<Result<Banner>> CreateBannerAsync(Banner banner, CancellationToken ct = default);
    Task<Result> UpdateBannerAsync(Banner banner, CancellationToken ct = default);
    Task<Result> DeleteBannerAsync(int id, CancellationToken ct = default);

    // Site Settings
    Task<Result<IEnumerable<SiteSetting>>> GetAllSiteSettingsAsync(CancellationToken ct = default);
    Task<Result<SiteSetting>> GetSiteSettingByIdAsync(int id, CancellationToken ct = default);
    Task<Result> UpdateSiteSettingAsync(SiteSetting setting, CancellationToken ct = default);
}
