using BusinessLayer.Interfaces;
using DataLayer.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AdminLayer.Controllers;

public class TestimonialsController : Controller
{
    private readonly ICmsService _cmsService;

    public TestimonialsController(ICmsService cmsService) => _cmsService = cmsService;

    public async Task<IActionResult> Index()
    {
        var result = await _cmsService.GetAllTestimonialsAsync();
        return View((result.Data ?? new List<Testimonial>()).OrderBy(t => t.DisplayOrder).ThenBy(t => t.StudentName).ToList());
    }

    public async Task<IActionResult> Preview(int id)
    {
        var result = await _cmsService.GetTestimonialByIdAsync(id);
        if (!result.IsSuccess || result.Data == null) return NotFound();
        return View(result.Data);
    }

    [HttpGet]
    public IActionResult Create() => View(new Testimonial { Rating = 5, IsVisible = true });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Testimonial testimonial)
    {
        ValidateTestimonial(testimonial);
        if (!ModelState.IsValid) return View(testimonial);

        var result = await _cmsService.CreateTestimonialAsync(testimonial);
        if (result.IsSuccess)
        {
            TempData["Success"] = "Testimonial created.";
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", result.ErrorMessage!);
        return View(testimonial);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _cmsService.GetTestimonialByIdAsync(id);
        if (!result.IsSuccess || result.Data == null) return NotFound();
        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Testimonial testimonial)
    {
        ValidateTestimonial(testimonial);
        if (!ModelState.IsValid) return View(testimonial);

        var existing = await _cmsService.GetTestimonialByIdAsync(id);
        if (!existing.IsSuccess || existing.Data == null) return NotFound();
        var item = existing.Data;
        item.StudentName = testimonial.StudentName;
        item.StudentRole = testimonial.StudentRole;
        item.ProfilePictureUrl = testimonial.ProfilePictureUrl;
        item.Content = testimonial.Content;
        item.Rating = testimonial.Rating;
        item.IsVisible = testimonial.IsVisible;
        item.DisplayOrder = testimonial.DisplayOrder;

        var result = await _cmsService.UpdateTestimonialAsync(item);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Testimonial updated." : result.ErrorMessage;
        return result.IsSuccess ? RedirectToAction(nameof(Index)) : View(testimonial);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVisibility(int id)
    {
        var result = await _cmsService.GetTestimonialByIdAsync(id);
        if (!result.IsSuccess || result.Data == null) return NotFound();
        result.Data.IsVisible = !result.Data.IsVisible;
        var update = await _cmsService.UpdateTestimonialAsync(result.Data);
        TempData[update.IsSuccess ? "Success" : "Error"] = update.IsSuccess ? (result.Data.IsVisible ? "Testimonial published." : "Testimonial unpublished.") : update.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _cmsService.DeleteTestimonialAsync(id);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Testimonial deleted." : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    private void ValidateTestimonial(Testimonial testimonial)
    {
        if (string.IsNullOrWhiteSpace(testimonial.StudentName)) ModelState.AddModelError(nameof(testimonial.StudentName), "Student name is required.");
        if (string.IsNullOrWhiteSpace(testimonial.Content)) ModelState.AddModelError(nameof(testimonial.Content), "Testimonial content is required.");
        if (testimonial.Rating < 1 || testimonial.Rating > 5) ModelState.AddModelError(nameof(testimonial.Rating), "Rating must be between 1 and 5.");
    }
}
