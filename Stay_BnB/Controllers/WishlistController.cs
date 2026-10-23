using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.Services;

namespace Stay_BnB.Controllers;

[Authorize(Roles = "Guest")]
public class WishlistController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogger _activityLogger;

    public WishlistController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogger activityLogger)
    {
        _context = context;
        _userManager = userManager;
        _activityLogger = activityLogger;
    }

    // GET: /Wishlist
    // Guest saved favorite properties
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var wishlists = await _context.WishlistItems
            .Include(w => w.Property)
                .ThenInclude(p => p!.Images)
            .Include(w => w.Property)
                .ThenInclude(p => p!.Reviews)
            .Where(w => w.UserId == user.Id && w.Property != null && w.Property.IsActive)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        return View(wishlists);
    }

    // POST: /Wishlist/Toggle?propertyId=5
    // Add or remove property from guest favorites
    [HttpPost]
    public async Task<IActionResult> Toggle(int propertyId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Unauthorized();
            }
            return Challenge();
        }

        var property = await _context.HostProperties.FirstOrDefaultAsync(p => p.Id == propertyId && p.IsActive);
        if (property == null) return NotFound();

        var existing = await _context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == user.Id && w.PropertyId == propertyId);

        bool isSaved;
        if (existing != null)
        {
            _context.WishlistItems.Remove(existing);
            isSaved = false;
        }
        else
        {
            _context.WishlistItems.Add(new WishlistItem
            {
                UserId = user.Id,
                PropertyId = propertyId,
                CreatedAt = DateTime.UtcNow
            });
            isSaved = true;
        }

        await _context.SaveChangesAsync();

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { saved = isSaved, message = isSaved ? "Saved to wishlist" : "Removed from wishlist" });
        }

        var returnUrl = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /Wishlist/Remove
    // Remove property from wishlist directly from the wishlist page
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int propertyId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var item = await _context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == user.Id && w.PropertyId == propertyId);

        if (item != null)
        {
            _context.WishlistItems.Remove(item);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Property removed from your wishlist.";
        }

        return RedirectToAction(nameof(Index));
    }
}
