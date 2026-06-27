using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UserLayer.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly IUserService _userService;

    public ProfileController(IUserService userService)
    {
        _userService = userService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = Guid.Parse(User.FindFirst("UserId")!.Value);
        ViewData["Title"] = "My Profile";

        var result = await _userService.GetUserByIdAsync(userId);
        if (!result.IsSuccess || result.Data == null) return NotFound();
        return View(result.Data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(DataLayer.Entities.User model)
    {
        var userId = Guid.Parse(User.FindFirst("UserId")!.Value);
        var existing = await _userService.GetUserByIdAsync(userId);
        if (!existing.IsSuccess || existing.Data == null) return NotFound();

        var user = existing.Data;
        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.Bio = model.Bio;
        user.Headline = model.Headline;
        user.WebsiteUrl = model.WebsiteUrl;
        user.LinkedInUrl = model.LinkedInUrl;

        var result = await _userService.UpdateProfileAsync(user);
        if (result.IsSuccess)
            TempData["Success"] = "Profile updated successfully!";
        else
            TempData["Error"] = result.ErrorMessage;

        return RedirectToAction(nameof(Index));
    }
}
