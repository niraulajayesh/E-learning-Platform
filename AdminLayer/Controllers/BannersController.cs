using BusinessLayer.Interfaces;
using DataLayer.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AdminLayer.Controllers;

public class BannersController : Controller
{
    private readonly ICmsService _cmsService;

    public BannersController(ICmsService cmsService) => _cmsService = cmsService;

    public async Task<IActionResult> Index()
    {
        var result = await _cmsService.GetAllBannersAsync();
        return View((result.Data ?? new List<Banner>()).OrderBy(b => b.DisplayOrder).ThenBy(b => b.Title).ToList());
    }

    public async Task<IActionResult> Preview(int id)
    {
        var result = await _cmsService.GetBannerByIdAsync(id);
        if (!result.IsSuccess || result.Data == null) return NotFound();
        return View(result.Data);
    }

    [HttpGet]
    public IActionResult Create() => View(new Banner { IsActive = true });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Banner banner)
    {
        ValidateBanner(banner);
        if (!ModelState.IsValid) return View(banner);

        var result = await _cmsService.CreateBannerAsync(banner);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Banner created.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", result.ErrorMessage!);
        return View(banner);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _cmsService.GetBannerByIdAsync(id);
        if (!result.IsSuccess || result.Data == null) return NotFound();
        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Banner banner)
    {
        ValidateBanner(banner);
        if (!ModelState.IsValid) return View(banner);

        var existing = await _cmsService.GetBannerByIdAsync(id);
        if (!existing.IsSuccess || existing.Data == null) return NotFound();
        var item = existing.Data;
        item.Title = banner.Title;
        item.Subtitle = banner.Subtitle;
        item.ImageUrl = banner.ImageUrl;
        item.ButtonText = banner.ButtonText;
        item.ButtonLink = banner.ButtonLink;
        item.IsActive = banner.IsActive;
        item.DisplayOrder = banner.DisplayOrder;

        var result = await _cmsService.UpdateBannerAsync(item);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Banner updated." : result.ErrorMessage;
        return result.IsSuccess ? RedirectToAction(nameof(Index)) : View(banner);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var result = await _cmsService.GetBannerByIdAsync(id);
        if (!result.IsSuccess || result.Data == null) return NotFound();
        result.Data.IsActive = !result.Data.IsActive;
        var update = await _cmsService.UpdateBannerAsync(result.Data);
        TempData[update.IsSuccess ? "Success" : "Error"] = update.IsSuccess ? (result.Data.IsActive ? "Banner published." : "Banner unpublished.") : update.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(int id, string direction)
    {
        var result = await _cmsService.GetBannerByIdAsync(id);
        if (!result.IsSuccess || result.Data == null) return NotFound();
        result.Data.DisplayOrder += direction == "up" ? -1 : 1;
        if (result.Data.DisplayOrder < 0) result.Data.DisplayOrder = 0;
        var update = await _cmsService.UpdateBannerAsync(result.Data);
        TempData[update.IsSuccess ? "Success" : "Error"] = update.IsSuccess ? "Banner order updated." : update.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _cmsService.DeleteBannerAsync(id);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Banner deleted." : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    private void ValidateBanner(Banner banner)
    {
        if (string.IsNullOrWhiteSpace(banner.Title)) ModelState.AddModelError(nameof(banner.Title), "Title is required.");
        if (string.IsNullOrWhiteSpace(banner.ImageUrl)) ModelState.AddModelError(nameof(banner.ImageUrl), "Image URL is required.");
    }
}
