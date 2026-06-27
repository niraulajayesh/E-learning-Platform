using BusinessLayer.Interfaces;
using DataLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminLayer.Controllers;

public class SiteSettingsController : Controller
{
    private readonly ICmsService _cmsService;

    public SiteSettingsController(ICmsService cmsService)
    {
        _cmsService = cmsService;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _cmsService.GetAllSiteSettingsAsync();
        return View(result.Data ?? new List<SiteSetting>());
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _cmsService.GetSiteSettingByIdAsync(id);
        if (!result.IsSuccess || result.Data == null) return NotFound();
        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SiteSetting setting)
    {
        var result = await _cmsService.UpdateSiteSettingAsync(setting);
        if (result.IsSuccess)
            return RedirectToAction(nameof(Index));

        ModelState.AddModelError("", result.ErrorMessage!);
        return View(setting);
    }
}




