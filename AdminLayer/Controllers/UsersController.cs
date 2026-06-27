using System.Security.Cryptography;
using System.Text;
using BusinessLayer.Interfaces;
using PlatformUser = DataLayer.Entities.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedLayer.Enums;

namespace AdminLayer.Controllers;

public class UsersController : Controller
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService) => _userService = userService;

    public async Task<IActionResult> Index(string? search, UserRole? role, bool? isActive)
    {
        var result = await _userService.GetAllUsersAsync();
        var users = result.Data ?? new List<PlatformUser>();
        if (!string.IsNullOrWhiteSpace(search)) users = users.Where(u => u.FullName.Contains(search, StringComparison.OrdinalIgnoreCase) || u.Email.Contains(search, StringComparison.OrdinalIgnoreCase));
        if (role.HasValue) users = users.Where(u => u.Role == role.Value);
        if (isActive.HasValue) users = users.Where(u => u.IsActive == isActive.Value);
        ViewBag.Search = search;
        ViewBag.Role = role;
        ViewBag.IsActive = isActive;
        return View(users.ToList());
    }

    [HttpGet]
    public IActionResult Create() => View(new PlatformUser { Role = UserRole.Student, IsActive = true, IsEmailVerified = true });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PlatformUser user, string password)
    {
        ClearUserModelState();
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6) ModelState.AddModelError(nameof(password), "Password must be at least 6 characters.");
        if (!ModelState.IsValid) return View(user);
        var result = await _userService.CreateUserAsync(user, HashPassword(password));
        if (result.IsSuccess) { TempData["Success"] = "User created."; return RedirectToAction(nameof(Index)); }
        ModelState.AddModelError("", result.ErrorMessage!);
        return View(user);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var result = await _userService.GetUserByIdAsync(id);
        if (!result.IsSuccess || result.Data == null) return NotFound();
        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PlatformUser user)
    {
        ClearUserModelState();
        if (!ModelState.IsValid) return View(user);
        var existingUser = await _userService.GetUserByIdAsync(user.Id);
        if (!existingUser.IsSuccess) return NotFound();
        var dbUser = existingUser.Data!;
        dbUser.FirstName = user.FirstName;
        dbUser.LastName = user.LastName;
        dbUser.Bio = user.Bio;
        dbUser.Headline = user.Headline;
        dbUser.WebsiteUrl = user.WebsiteUrl;
        dbUser.LinkedInUrl = user.LinkedInUrl;
        dbUser.Role = user.Role;
        dbUser.IsActive = user.IsActive;
        var result = await _userService.UpdateProfileAsync(dbUser);
        if (result.IsSuccess) { TempData["Success"] = "User updated."; return RedirectToAction(nameof(Index)); }
        ModelState.AddModelError("", result.ErrorMessage!);
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(Guid id, bool isActive)
    {
        var result = await _userService.SetActiveStatusAsync(id, isActive);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? (isActive ? "User activated." : "User suspended.") : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteInstructor(Guid id)
    {
        var result = await _userService.SetRoleAsync(id, UserRole.Instructor);
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "User promoted to instructor." : result.ErrorMessage;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(Guid id, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            TempData["Error"] = "Password must be at least 6 characters.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        var result = await _userService.ResetPasswordAsync(id, HashPassword(newPassword));
        TempData[result.IsSuccess ? "Success" : "Error"] = result.IsSuccess ? "Password reset." : result.ErrorMessage;
        return RedirectToAction(nameof(Edit), new { id });
    }

    private void ClearUserModelState()
    {
        ModelState.Remove(nameof(PlatformUser.PasswordHash));
        ModelState.Remove(nameof(PlatformUser.CoursesCreated));
        ModelState.Remove(nameof(PlatformUser.Enrollments));
        ModelState.Remove(nameof(PlatformUser.Reviews));
        ModelState.Remove(nameof(PlatformUser.Payments));
        ModelState.Remove(nameof(PlatformUser.QuizAttempts));
        ModelState.Remove(nameof(PlatformUser.Notifications));
        ModelState.Remove(nameof(PlatformUser.Certificates));
    }

    private static string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(password)));
    }
}

