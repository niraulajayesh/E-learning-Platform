using BusinessLayer.Interfaces;
using DataLayer.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AdminLayer.Controllers;

public class CategoriesController : Controller
{
    private const int PageSize = 10;
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var result = await _categoryService.GetAllCategoriesAsync();
        var categories = result.Data ?? Enumerable.Empty<Category>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            categories = categories.Where(c => c.Name.Contains(search, StringComparison.OrdinalIgnoreCase) || c.Slug.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var total = categories.Count();
        page = Math.Max(1, page);
        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        ViewBag.TotalItems = total;

        return View(categories.Skip((page - 1) * PageSize).Take(PageSize).ToList());
    }

    public async Task<IActionResult> Details(int id)
    {
        var result = await _categoryService.GetCategoryByIdAsync(id);
        if (!result.IsSuccess || result.Data == null) return NotFound();
        return View(result.Data);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new Category { IsActive = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Category category)
    {
        ClearCategoryModelState();
        ValidateCategory(category);

        if (ModelState.IsValid)
        {
            var result = await _categoryService.CreateCategoryAsync(category);
            if (result.IsSuccess)
            {
                TempData["Success"] = "Category created.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", result.ErrorMessage!);
        }
        return View(category);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var result = await _categoryService.GetCategoryByIdAsync(id);
        if (!result.IsSuccess || result.Data == null) return NotFound();
        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Category category)
    {
        ClearCategoryModelState();
        ValidateCategory(category);

        if (ModelState.IsValid)
        {
            var result = await _categoryService.UpdateCategoryAsync(category);
            if (result.IsSuccess)
            {
                TempData["Success"] = "Category updated.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", result.ErrorMessage!);
        }
        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, bool confirmWithCourses = false)
    {
        var result = await _categoryService.DeleteCategoryAsync(id, confirmWithCourses);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Category deleted or deactivated." : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    private void ValidateCategory(Category category)
    {
        if (string.IsNullOrWhiteSpace(category.Name)) ModelState.AddModelError(nameof(Category.Name), "Name is required.");
        if (category.Name?.Length > 100) ModelState.AddModelError(nameof(Category.Name), "Name must be 100 characters or fewer.");
        if (category.Slug?.Length > 120) ModelState.AddModelError(nameof(Category.Slug), "Slug must be 120 characters or fewer.");
    }

    private void ClearCategoryModelState()
    {
        ModelState.Remove(nameof(Category.ParentCategory));
        ModelState.Remove(nameof(Category.SubCategories));
        ModelState.Remove(nameof(Category.Courses));
    }
}


