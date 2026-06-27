using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Mvc;
using SharedLayer.Enums;
using UserLayer.Models;

namespace UserLayer.Controllers;

public class InstructorsController : Controller
{
    private readonly IUserService _userService;
    private readonly ICourseService _courseService;

    public InstructorsController(IUserService userService, ICourseService courseService)
    {
        _userService = userService;
        _courseService = courseService;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Meet Our Instructors";

        var instructorsResult = await _userService.GetAllInstructorsAsync();
        var publishedCoursesResult = await _courseService.GetAllPublishedCoursesAsync();
        var publishedCourses = (publishedCoursesResult.Data ?? Enumerable.Empty<DataLayer.Entities.Course>()).ToList();

        var instructorsById = (instructorsResult.Data ?? Enumerable.Empty<DataLayer.Entities.User>())
            .ToDictionary(i => i.Id, i => i);

        foreach (var course in publishedCourses.Where(c => c.Instructor != null))
        {
            instructorsById.TryAdd(course.InstructorId, course.Instructor);
        }

        var model = instructorsById.Values
            .Select(instructor =>
            {
                var instructorCourses = publishedCourses
                    .Where(c => c.InstructorId == instructor.Id && c.Status == CourseStatus.Published)
                    .ToList();

                return new InstructorCardViewModel
                {
                    Instructor = instructor,
                    PublishedCourseCount = instructorCourses.Count,
                    TotalStudents = instructorCourses.Sum(c => c.TotalEnrollments),
                    AverageRating = instructorCourses
                        .Where(c => c.AverageRating > 0)
                        .Select(c => c.AverageRating)
                        .DefaultIfEmpty(0)
                        .Average()
                };
            })
            .OrderByDescending(i => i.PublishedCourseCount)
            .ThenBy(i => i.Instructor.FirstName)
            .ToList();

        return View(model);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var instructorResult = await _userService.GetUserByIdAsync(id);
        if (!instructorResult.IsSuccess || instructorResult.Data == null) return NotFound();

        var instructor = instructorResult.Data;
        ViewData["Title"] = instructor.FullName;

        var coursesResult = await _courseService.GetCoursesByInstructorAsync(id);
        var courses = (coursesResult.Data ?? Enumerable.Empty<DataLayer.Entities.Course>())
            .Where(c => c.Status == CourseStatus.Published)
            .OrderByDescending(c => c.IsFeatured)
            .ThenByDescending(c => c.TotalEnrollments)
            .ToList();

        foreach (var course in courses)
        {
            course.Instructor = instructor;
        }

        return View(new InstructorProfileViewModel
        {
            Instructor = instructor,
            Courses = courses,
            TotalStudents = courses.Sum(c => c.TotalEnrollments),
            AverageRating = courses
                .Where(c => c.AverageRating > 0)
                .Select(c => c.AverageRating)
                .DefaultIfEmpty(0)
                .Average()
        });
    }
}
