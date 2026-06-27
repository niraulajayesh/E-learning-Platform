using DataLayer.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UserLayer.Controllers;

public class FlashcardsController : Controller
{
    private readonly AppDbContext _db;

    public FlashcardsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? categoryId, string? q = null)
    {
        var search = q?.Trim();
        var categories = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
        var query = _db.FlashcardSets.Include(s => s.Category).Include(s => s.Flashcards).Where(s => s.IsPublished);

        if (categoryId.HasValue) query = query.Where(s => s.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s =>
                s.Title.Contains(search) ||
                s.Description.Contains(search) ||
                s.Category.Name.Contains(search) ||
                s.Flashcards.Any(c => c.Front.Contains(search) || c.Back.Contains(search) || (c.Hint != null && c.Hint.Contains(search))));
        }

        ViewBag.Categories = categories;
        ViewBag.CategoryId = categoryId;
        ViewBag.SearchQuery = search;
        ViewData["Title"] = "ASVAB Flashcards";
        return View(await query.OrderBy(s => s.Category.DisplayOrder).ThenBy(s => s.Title).ToListAsync());
    }

    public async Task<IActionResult> Study(Guid id)
    {
        var set = await _db.FlashcardSets.Include(s => s.Category).Include(s => s.Flashcards.OrderBy(c => c.Order)).FirstOrDefaultAsync(s => s.Id == id && s.IsPublished);
        if (set == null) return NotFound();
        ViewData["Title"] = set.Title;
        return View(set);
    }
}
