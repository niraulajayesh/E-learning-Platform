using System.Diagnostics;
using DataLayer.Context;
using Microsoft.EntityFrameworkCore;
using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using UserLayer.Models;

namespace UserLayer.Controllers;

public class HomeController : Controller
{
    private readonly ICourseService _courseService;
    private readonly ICategoryService _categoryService;
    private readonly ICmsService _cmsService;
    private readonly AppDbContext _db;

    public HomeController(ICourseService courseService, ICategoryService categoryService, ICmsService cmsService, AppDbContext db)
    {
        _courseService = courseService;
        _categoryService = categoryService;
        _cmsService = cmsService;
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "ASVAB Exam Prep - LearnHub";

        var coursesResult = await _courseService.GetAllPublishedCoursesAsync();
        var categoriesResult = await _categoryService.GetAllCategoriesAsync();
        var testimonialsResult = await _cmsService.GetAllTestimonialsAsync();

        var asvabCategories = (categoriesResult.Data ?? Enumerable.Empty<DataLayer.Entities.Category>())
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToList();

        var allCourses = (coursesResult.Data ?? Enumerable.Empty<DataLayer.Entities.Course>()).ToList();
        var featuredCourses = allCourses.Where(c => c.IsFeatured).Take(8).ToList();
        var bestsellerCourses = allCourses.Where(c => c.IsBestseller).Take(6).ToList();

        var publishedPracticeTestsQuery = _db.PracticeTests.Where(t => t.IsPublished);
        var publishedStudyGuidesQuery = _db.StudyGuides.Where(g => g.IsPublished);
        var publishedFlashcardSetsQuery = _db.FlashcardSets.Where(s => s.IsPublished);

        var practiceTests = await publishedPracticeTestsQuery
            .Include(t => t.Category)
            .Include(t => t.Questions)
            .OrderBy(t => t.Category.DisplayOrder)
            .ThenBy(t => t.DisplayOrder)
            .Take(6)
            .ToListAsync();

        var studyGuides = await publishedStudyGuidesQuery
            .Include(g => g.Category)
            .OrderBy(g => g.Category.DisplayOrder)
            .ThenBy(g => g.Title)
            .Take(6)
            .ToListAsync();

        var flashcardSets = await publishedFlashcardSetsQuery
            .Include(s => s.Category)
            .OrderBy(s => s.Category.DisplayOrder)
            .ThenBy(s => s.Title)
            .Take(6)
            .ToListAsync();

        ViewBag.FeaturedCourses = featuredCourses;
        ViewBag.BestsellerCourses = bestsellerCourses;
        ViewBag.Categories = asvabCategories;
        ViewBag.Testimonials = (testimonialsResult.Data ?? Enumerable.Empty<DataLayer.Entities.Testimonial>())
            .Where(t => t.IsVisible)
            .Take(6)
            .ToList();
        ViewBag.PracticeTests = practiceTests;
        ViewBag.StudyGuides = studyGuides;
        ViewBag.FlashcardSets = flashcardSets;

        ViewBag.TotalCourses = allCourses.Count;
        ViewBag.TotalTests = await publishedPracticeTestsQuery.CountAsync();
        ViewBag.TotalGuides = await publishedStudyGuidesQuery.CountAsync();
        ViewBag.TotalFlashcards = await publishedFlashcardSetsQuery.CountAsync();

        var faq = new[]
        {
            new { Question = "How is this better than generic online courses?", Answer = "It is ASVAB-specific: every resource is aligned to WK, PC, AR, MK, GS, EI, MC, AS, and AO and mapped to your real exam goals." },
            new { Question = "What is the recommended prep schedule?", Answer = "Use the Study Planner with daily goals, mix lessons, timed practice, and flashcard review. Most students improve in 6-12 weeks with 45-120 minutes/day." },
            new { Question = "Are practice tests timed and realistic?", Answer = "Yes. We provide section-aligned ASVAB-style practice with realistic timing, distractors, and score feedback." },
            new { Question = "Can I review specific weak areas only?", Answer = "Yes. Use your dashboard analytics and the recommended focus list to jump straight to weak categories." },
            new { Question = "Is this platform updated for ASVAB syllabus changes?", Answer = "Content is built to cover the same official ASVAB knowledge areas and is reviewed with current test patterns in mind." }
        };
        ViewBag.Faq = faq.ToList();

        return View();
    }
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [Route("Home/StatusCode/{statusCode:int}")]
    public IActionResult HandleStatusCode(int statusCode)
    {
        Response.StatusCode = statusCode;
        ViewData["Title"] = statusCode switch
        {
            StatusCodes.Status403Forbidden => "Access denied",
            StatusCodes.Status404NotFound => "Page not found",
            _ => "Something went wrong"
        };
        return View("StatusCode", statusCode);
    }
}
