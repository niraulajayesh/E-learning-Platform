using BusinessLayer.Interfaces;
using DataLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminLayer.Controllers;

public class TestimonialsController : Controller
{
    private readonly ICmsService _cmsService;

    public TestimonialsController(ICmsService cmsService)
    {
        _cmsService = cmsService;
    }

    public async Task<IActionResult> Index()
    {
        var result = await _cmsService.GetAllTestimonialsAsync();
        return View(result.Data ?? new List<Testimonial>());
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new Testimonial { Rating = 5, IsVisible = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Testimonial testimonial)
    {
        if (testimonial.Rating < 1 || testimonial.Rating > 5)
            ModelState.AddModelError(nameof(Testimonial.Rating), "Rating must be between 1 and 5.");

        if (!ModelState.IsValid)
            return View(testimonial);

        var result = await _cmsService.CreateTestimonialAsync(testimonial);
        if (result.IsSuccess)
            return RedirectToAction(nameof(Index));

        ModelState.AddModelError("", result.ErrorMessage!);
        return View(testimonial);
    }
}



