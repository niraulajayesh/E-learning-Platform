using DataLayer.Context;
using DataLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdminLayer.Controllers;

public class StudyGuidesController : Controller
{
    private readonly AppDbContext _db;

    public StudyGuidesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? search, int? categoryId)
    {
        var query = _db.StudyGuides.Include(g => g.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(g => g.Title.Contains(search) || g.Summary.Contains(search));
        if (categoryId.HasValue) query = query.Where(g => g.CategoryId == categoryId.Value);
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        await LoadCategories();
        return View(await query.OrderBy(g => g.Category.DisplayOrder).ThenBy(g => g.DisplayOrder).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        await LoadCategories();
        return View(new StudyGuide { IsPublished = true, DisplayOrder = 1 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudyGuide guide)
    {
        ValidateGuide(guide);
        if (!ModelState.IsValid)
        {
            await LoadCategories();
            return View(guide);
        }

        guide.Id = Guid.NewGuid();
        _db.StudyGuides.Add(guide);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Study guide created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var guide = await _db.StudyGuides.FindAsync(id);
        if (guide == null) return NotFound();
        await LoadCategories();
        return View(guide);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, StudyGuide model)
    {
        ValidateGuide(model);
        if (!ModelState.IsValid)
        {
            model.Id = id;
            await LoadCategories();
            return View(model);
        }

        var guide = await _db.StudyGuides.FindAsync(id);
        if (guide == null) return NotFound();
        guide.CategoryId = model.CategoryId;
        guide.Title = model.Title;
        guide.Summary = model.Summary;
        guide.Theory = model.Theory;
        guide.Examples = model.Examples;
        guide.KeyConcepts = model.KeyConcepts;
        guide.Tips = model.Tips;
        guide.IsPublished = model.IsPublished;
        guide.DisplayOrder = model.DisplayOrder;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Study guide updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var guide = await _db.StudyGuides.Include(g => g.Bookmarks).FirstOrDefaultAsync(g => g.Id == id);
        if (guide != null)
        {
            _db.StudyGuideBookmarks.RemoveRange(guide.Bookmarks);
            _db.StudyGuides.Remove(guide);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Study guide deleted.";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCategories() => ViewBag.Categories = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();

    private void ValidateGuide(StudyGuide guide)
    {
        if (guide.CategoryId <= 0) ModelState.AddModelError(nameof(guide.CategoryId), "Category is required.");
        if (string.IsNullOrWhiteSpace(guide.Title)) ModelState.AddModelError(nameof(guide.Title), "Title is required.");
        if (string.IsNullOrWhiteSpace(guide.Theory)) ModelState.AddModelError(nameof(guide.Theory), "Theory is required.");
        if (string.IsNullOrWhiteSpace(guide.Examples)) ModelState.AddModelError(nameof(guide.Examples), "Examples are required.");
        if (string.IsNullOrWhiteSpace(guide.KeyConcepts)) ModelState.AddModelError(nameof(guide.KeyConcepts), "Key concepts are required.");
        if (string.IsNullOrWhiteSpace(guide.Tips)) ModelState.AddModelError(nameof(guide.Tips), "Tips are required.");
        ModelState.Remove(nameof(guide.Category));
        ModelState.Remove(nameof(guide.Bookmarks));
    }
}

