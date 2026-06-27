using DataLayer.Context;
using DataLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdminLayer.Controllers;

public class FlashcardsController : Controller
{
    private readonly AppDbContext _db;

    public FlashcardsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? search, int? categoryId)
    {
        var query = _db.FlashcardSets.Include(s => s.Category).Include(s => s.Flashcards).AsQueryable();
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(s => s.Title.Contains(search) || s.Description.Contains(search));
        if (categoryId.HasValue) query = query.Where(s => s.CategoryId == categoryId.Value);
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.Categories = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
        return View(await query.OrderBy(s => s.Category.DisplayOrder).ThenBy(s => s.Title).ToListAsync());
    }

    public async Task<IActionResult> Create()
    {
        await LoadCategories();
        return View(new FlashcardSet { IsPublished = true, Cards = DefaultCards() });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FlashcardSet model)
    {
        NormalizeCards(model);
        if (!ValidateSet(model))
        {
            await LoadCategories();
            return View(model);
        }

        model.Id = Guid.NewGuid();
        foreach (var card in model.Cards) card.Id = Guid.NewGuid();
        _db.FlashcardSets.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Flashcard set created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var set = await _db.FlashcardSets.Include(s => s.Cards.OrderBy(c => c.Order)).FirstOrDefaultAsync(s => s.Id == id);
        if (set == null) return NotFound();
        await LoadCategories();
        return View(set);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, FlashcardSet model)
    {
        NormalizeCards(model);
        if (!ValidateSet(model))
        {
            model.Id = id;
            await LoadCategories();
            return View(model);
        }

        var set = await _db.FlashcardSets.Include(s => s.Cards).FirstOrDefaultAsync(s => s.Id == id);
        if (set == null) return NotFound();
        set.CategoryId = model.CategoryId;
        set.Title = model.Title;
        set.Description = model.Description;
        set.IsPublished = model.IsPublished;
        _db.Flashcards.RemoveRange(set.Cards);
        set.Cards = model.Cards.Select((c, i) => new Flashcard { Id = Guid.NewGuid(), FlashcardSetId = set.Id, Front = c.Front, Back = c.Back, Hint = c.Hint, Order = i + 1 }).ToList();
        await _db.SaveChangesAsync();
        TempData["Success"] = "Flashcard set updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var set = await _db.FlashcardSets.Include(s => s.Cards).FirstOrDefaultAsync(s => s.Id == id);
        if (set != null)
        {
            _db.Flashcards.RemoveRange(set.Cards);
            _db.FlashcardSets.Remove(set);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Flashcard set deleted.";
        }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCard(Guid id, Guid setId)
    {
        var card = await _db.Flashcards.FirstOrDefaultAsync(c => c.Id == id && c.FlashcardSetId == setId);
        if (card == null) return NotFound();
        _db.Flashcards.Remove(card);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Flashcard deleted.";
        return RedirectToAction(nameof(Edit), new { id = setId });
    }
    private async Task LoadCategories() => ViewBag.Categories = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();

    private bool ValidateSet(FlashcardSet model)
    {
        if (string.IsNullOrWhiteSpace(model.Title)) ModelState.AddModelError(nameof(model.Title), "Title is required.");
        if (model.CategoryId <= 0) ModelState.AddModelError(nameof(model.CategoryId), "Category is required.");
        if (!model.Cards.Any()) ModelState.AddModelError("Cards", "At least one flashcard is required.");
        return ModelState.IsValid;
    }

    private static void NormalizeCards(FlashcardSet model)
    {
        model.Cards = model.Cards.Where(c => !string.IsNullOrWhiteSpace(c.Front) && !string.IsNullOrWhiteSpace(c.Back)).Select((c, i) => { c.Order = i + 1; return c; }).ToList();
    }

    private static List<Flashcard> DefaultCards() => Enumerable.Range(1, 5).Select(i => new Flashcard { Order = i }).ToList();
}


