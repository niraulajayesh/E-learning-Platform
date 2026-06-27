using System.Diagnostics;
using System.Reflection;
using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AdminLayer.Models;

namespace AdminLayer.Controllers;

public class HomeController : Controller
{
    private readonly IDashboardService _dashboardService;

    public HomeController(IDashboardService dashboardService) => _dashboardService = dashboardService;

    public async Task<IActionResult> Index()
    {
        var metricsResult = await _dashboardService.GetAdminDashboardMetricsAsync();
        var metrics = metricsResult.Data;
        ViewBag.TotalUsers = GetMetric(metrics, "TotalUsers") ?? 0;
        ViewBag.TotalStudents = GetMetric(metrics, "TotalStudents") ?? 0;
        ViewBag.TotalInstructors = GetMetric(metrics, "TotalInstructors") ?? 0;
        ViewBag.TotalCourses = GetMetric(metrics, "TotalCourses") ?? 0;
        ViewBag.ActiveEnrollments = GetMetric(metrics, "ActiveEnrollments") ?? 0;
        ViewBag.TotalEnrollments = GetMetric(metrics, "TotalEnrollments") ?? 0;
        ViewBag.TotalRevenue = GetMetric(metrics, "TotalRevenue") ?? 0m;
        ViewBag.CourseCompletionRate = GetMetric(metrics, "CourseCompletionRate") ?? 0;
        ViewBag.QuizPassRate = GetMetric(metrics, "QuizPassRate") ?? 0;
        return View();
    }

    private static object? GetMetric(object? source, string name)
        => source?.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(source);
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        Response.StatusCode = StatusCodes.Status500InternalServerError;
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [AllowAnonymous]
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

