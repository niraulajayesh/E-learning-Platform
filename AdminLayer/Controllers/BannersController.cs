using BusinessLayer.Interfaces;
using DataLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminLayer.Controllers;

public class BannersController : Controller
{
    private readonly ICmsService _cmsService;

    public BannersController(ICmsService cmsService)
    {
        _cmsService = cmsService;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _cmsService.GetAllBannersAsync();
        return View(result.Data ?? new List<Banner>());
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new Banner { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Banner banner)
    {
        if (!ModelState.IsValid)
            return View(banner);

        var result = await _cmsService.CreateBannerAsync(banner);
        if (result.IsSuccess)
            return RedirectToAction(nameof(Index));

        ModelState.AddModelError("", result.ErrorMessage!);
        return View(banner);
    }
}



