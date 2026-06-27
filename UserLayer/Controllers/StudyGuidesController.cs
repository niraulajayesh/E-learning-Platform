using System.Security.Claims;
using DataLayer.Context;
using DataLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UserLayer.Controllers;

public class StudyGuidesController : Controller
{
    private readonly AppDbContext _db;

    public StudyGuidesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? categoryId, bool bookmarks = false, string? q = null)
    {
        var search = q?.Trim();
        var categories = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
        var query = _db.StudyGuides.Include(g => g.Category).Include(g => g.Bookmarks).Where(g => g.IsPublished);

        if (categoryId.HasValue) query = query.Where(g => g.CategoryId == categoryId.Value);
        if (bookmarks && TryGetCurrentUserId(out var userId)) query = query.Where(g => g.Bookmarks.Any(b => b.StudentId == userId));
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(g =>
                g.Title.Contains(search) ||
                g.Summary.Contains(search) ||
                g.Category.Name.Contains(search) ||
                g.Theory.Contains(search) ||
                g.Examples.Contains(search) ||
                g.KeyConcepts.Contains(search) ||
                g.Tips.Contains(search));
        }

        ViewBag.Categories = categories;
        ViewBag.CategoryId = categoryId;
        ViewBag.BookmarksOnly = bookmarks;
        ViewBag.SearchQuery = search;
        ViewBag.BookmarkedGuideIds = TryGetCurrentUserId(out var bookmarkUserId)
            ? await _db.StudyGuideBookmarks.Where(b => b.StudentId == bookmarkUserId).Select(b => b.StudyGuideId).ToListAsync()
            : new List<Guid>();
        ViewData["Title"] = "ASVAB Study Guides";
        return View(await query.OrderBy(g => g.Category.DisplayOrder).ThenBy(g => g.DisplayOrder).ToListAsync());
    }

    public async Task<IActionResult> Read(Guid id)
    {
        var guide = await _db.StudyGuides.Include(g => g.Category).Include(g => g.Bookmarks).FirstOrDefaultAsync(g => g.Id == id && g.IsPublished);
        if (guide == null) return NotFound();

        var siblings = await _db.StudyGuides.Include(g => g.Category)
            .Where(g => g.IsPublished && g.CategoryId == guide.CategoryId)
            .OrderBy(g => g.DisplayOrder)
            .ThenBy(g => g.Title)
            .ToListAsync();
        var index = siblings.FindIndex(g => g.Id == guide.Id);

        ViewBag.IsBookmarked = TryGetCurrentUserId(out var userId) && guide.Bookmarks.Any(b => b.StudentId == userId);
        ViewBag.PreviousGuide = index > 0 ? siblings[index - 1] : null;
        ViewBag.NextGuide = index >= 0 && index < siblings.Count - 1 ? siblings[index + 1] : null;
        ViewBag.RelatedPracticeTests = await _db.PracticeTests.Include(t => t.Category).Include(t => t.Questions)
            .Where(t => t.IsPublished && t.CategoryId == guide.CategoryId)
            .OrderBy(t => t.DisplayOrder)
            .Take(3)
            .ToListAsync();
        ViewBag.RelatedFlashcardSets = await _db.FlashcardSets.Include(s => s.Category).Include(s => s.Flashcards)
            .Where(s => s.IsPublished && s.CategoryId == guide.CategoryId)
            .OrderBy(s => s.Title)
            .Take(3)
            .ToListAsync();
        ViewData["Title"] = guide.Title;
        return View(guide);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bookmark(Guid id)
    {
        if (!TryGetCurrentUserId(out var userId)) return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action(nameof(Read), new { id }) });
        var existing = await _db.StudyGuideBookmarks.FirstOrDefaultAsync(b => b.StudyGuideId == id && b.StudentId == userId);
        if (existing == null) _db.StudyGuideBookmarks.Add(new StudyGuideBookmark { StudyGuideId = id, StudentId = userId });
        else _db.StudyGuideBookmarks.Remove(existing);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Read), new { id });
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        var value = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out userId);
    }
}


