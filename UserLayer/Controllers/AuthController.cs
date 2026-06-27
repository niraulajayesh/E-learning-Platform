using BusinessLayer.Interfaces;
using DataLayer.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SharedLayer.Enums;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace UserLayer.Controllers;

public class AuthController : Controller
{
    private readonly IUserService _userService;

    // Constructor using UserService only (no direct UoW needed for basic auth)
    public AuthController(IUserService userService)
    {
        _userService = userService;
    }

    // ── GET Login ─────────────────────────────────────
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");

        ViewData["ReturnUrl"] = returnUrl;
        ViewData["Title"] = "Sign In";
        return View();
    }

    // ── POST Login ────────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        ViewData["Title"] = "Sign In";
        var userResult = await _userService.GetUserByEmailAsync(email);

        if (!userResult.IsSuccess || userResult.Data == null)
        {
            TempData["Error"] = "No account found with that email address.";
            return View();
        }

        var user = userResult.Data;
        var hashedInput = HashPassword(password);

        if (user.PasswordHash != hashedInput)
        {
            TempData["Error"] = "Incorrect password. Please try again.";
            return View();
        }

        if (!user.IsActive)
        {
            TempData["Error"] = "Your account has been deactivated. Please contact support.";
            return View();
        }

        await SignInUserAsync(user);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction("Index", "Dashboard");
    }

    // ── GET Register ──────────────────────────────────
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");
        ViewData["Title"] = "Create Account";
        return View();
    }

    // ── POST Register ─────────────────────────────────
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(string firstName, string lastName, string email, string password, string confirmPassword)
    {
        ViewData["Title"] = "Create Account";

        if (password != confirmPassword)
        {
            TempData["Error"] = "Passwords do not match.";
            return View();
        }

        var existingUser = await _userService.GetUserByEmailAsync(email);
        if (existingUser.IsSuccess && existingUser.Data != null)
        {
            TempData["Error"] = "An account with that email already exists.";
            return View();
        }

        var registerResult = await _userService.RegisterStudentAsync(firstName, lastName, email, HashPassword(password));
        if (!registerResult.IsSuccess || registerResult.Data == null)
        {
            TempData["Error"] = registerResult.ErrorMessage;
            return View();
        }

        await SignInUserAsync(registerResult.Data);
        TempData["Success"] = "Account created. Welcome to LearnHub!";
        return RedirectToAction("Index", "Dashboard");
    }

    // ── Logout ────────────────────────────────────────
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["Success"] = "You have been signed out. See you soon!";
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    // ── Helpers ───────────────────────────────────────
    private async Task SignInUserAsync(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("UserId", user.Id.ToString()),
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes);
    }
}

