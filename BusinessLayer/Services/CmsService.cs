using BusinessLayer.Interfaces;
using DataLayer.Entities;
using DataLayer.UnitOfWork;
using SharedLayer.Wrappers;

namespace BusinessLayer.Services;

public class CmsService : ICmsService
{
    private readonly IUnitOfWork _uow;

    public CmsService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Result<IEnumerable<Testimonial>>> GetAllTestimonialsAsync(CancellationToken ct = default)
    {
        var result = await _uow.Testimonials.GetAllAsync(ct);
        return Result<IEnumerable<Testimonial>>.Success(result);
    }

    public async Task<Result<Testimonial>> GetTestimonialByIdAsync(int id, CancellationToken ct = default)
    {
        var result = await _uow.Testimonials.GetFirstOrDefaultAsync(t => t.Id == id, ct);
        if (result == null) return Result<Testimonial>.Failure("Testimonial not found.");
        return Result<Testimonial>.Success(result);
    }

    public async Task<Result<Testimonial>> CreateTestimonialAsync(Testimonial testimonial, CancellationToken ct = default)
    {
        await _uow.Testimonials.AddAsync(testimonial, ct);
        await _uow.SaveChangesAsync(ct);
        return Result<Testimonial>.Success(testimonial);
    }

    public async Task<Result> UpdateTestimonialAsync(Testimonial testimonial, CancellationToken ct = default)
    {
        _uow.Testimonials.Update(testimonial);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteTestimonialAsync(int id, CancellationToken ct = default)
    {
        var existing = await _uow.Testimonials.GetFirstOrDefaultAsync(t => t.Id == id, ct);
        if (existing == null) return Result.Failure("Not found.");
        _uow.Testimonials.Remove(existing);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<IEnumerable<Banner>>> GetAllBannersAsync(CancellationToken ct = default)
    {
        var result = await _uow.Banners.GetAllAsync(ct);
        return Result<IEnumerable<Banner>>.Success(result);
    }

    public async Task<Result<Banner>> GetBannerByIdAsync(int id, CancellationToken ct = default)
    {
        var result = await _uow.Banners.GetFirstOrDefaultAsync(b => b.Id == id, ct);
        if (result == null) return Result<Banner>.Failure("Banner not found.");
        return Result<Banner>.Success(result);
    }

    public async Task<Result<Banner>> CreateBannerAsync(Banner banner, CancellationToken ct = default)
    {
        await _uow.Banners.AddAsync(banner, ct);
        await _uow.SaveChangesAsync(ct);
        return Result<Banner>.Success(banner);
    }

    public async Task<Result> UpdateBannerAsync(Banner banner, CancellationToken ct = default)
    {
        _uow.Banners.Update(banner);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteBannerAsync(int id, CancellationToken ct = default)
    {
        var existing = await _uow.Banners.GetFirstOrDefaultAsync(b => b.Id == id, ct);
        if (existing == null) return Result.Failure("Not found.");
        _uow.Banners.Remove(existing);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<IEnumerable<SiteSetting>>> GetAllSiteSettingsAsync(CancellationToken ct = default)
    {
        var result = await _uow.SiteSettings.GetAllAsync(ct);
        return Result<IEnumerable<SiteSetting>>.Success(result);
    }

    public async Task<Result<SiteSetting>> GetSiteSettingByIdAsync(int id, CancellationToken ct = default)
    {
        var result = await _uow.SiteSettings.GetFirstOrDefaultAsync(s => s.Id == id, ct);
        if (result == null) return Result<SiteSetting>.Failure("Setting not found.");
        return Result<SiteSetting>.Success(result);
    }

    public async Task<Result> UpdateSiteSettingAsync(SiteSetting setting, CancellationToken ct = default)
    {
        _uow.SiteSettings.Update(setting);
        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
