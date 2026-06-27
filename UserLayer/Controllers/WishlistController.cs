using BusinessLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace UserLayer.Controllers;

[Authorize]
public class WishlistController : Controller
{
    private readonly IWishlistService _wishlistService;

    public WishlistController(IWishlistService wishlistService)
    {
        _wishlistService = wishlistService;
    }

    public async Task<IActionResult> Index()
    {
        var studentId = Guid.Parse(User.FindFirst("UserId")!.Value);
        ViewData["Title"] = "My Wishlist";

        var result = await _wishlistService.GetStudentWishlistAsync(studentId);
        return View(result.Data ?? Enumerable.Empty<DataLayer.Entities.Wishlist>());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(Guid courseId)
    {
        var studentId = Guid.Parse(User.FindFirst("UserId")!.Value);
        var result = await _wishlistService.AddToWishlistAsync(studentId, courseId);

        if (result.IsSuccess)
            TempData["Success"] = "Course added to your wishlist!";
        else
            TempData["Error"] = result.ErrorMessage;

        return RedirectBack();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid courseId)
    {
        var studentId = Guid.Parse(User.FindFirst("UserId")!.Value);
        var result = await _wishlistService.RemoveFromWishlistAsync(studentId, courseId);

        if (result.IsSuccess)
            TempData["Success"] = "Course removed from wishlist.";
        else
            TempData["Error"] = result.ErrorMessage;

        return RedirectToAction(nameof(Index));
    }

    private IActionResult RedirectBack()
    {
        var referer = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(referer)) return Redirect(referer);
        return RedirectToAction(nameof(Index));
    }
}
