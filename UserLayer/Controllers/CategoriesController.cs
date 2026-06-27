using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace UserLayer.Controllers;

public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;
    private readonly ICourseService _courseService;

    public CategoriesController(ICategoryService categoryService, ICourseService courseService)
    {
        _categoryService = categoryService;
        _courseService = courseService;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Browse Categories";
        var result = await _categoryService.GetAllCategoriesAsync();
        return View(result.Data ?? Enumerable.Empty<DataLayer.Entities.Category>());
    }

    public async Task<IActionResult> Details(int id)
    {
        var catResult = await _categoryService.GetCategoryByIdAsync(id);
        if (!catResult.IsSuccess || catResult.Data == null) return NotFound();

        var category = catResult.Data;
        ViewData["Title"] = $"{category.Name} Courses";

        var coursesResult = await _courseService.SearchCoursesAsync("");
        var courses = (coursesResult.Data ?? Enumerable.Empty<DataLayer.Entities.Course>())
            .Where(c => c.CategoryId == id).ToList();

        ViewBag.Category = category;
        ViewBag.Courses = courses;
        return View();
    }
}
