using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.Models.Enums;
using Stay_BnB.Services;

namespace Stay_BnB.Controllers;

[Authorize(Roles = "Guest")]
public class ReviewsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogger _activityLogger;

    public ReviewsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogger activityLogger)
    {
        _context = context;
        _userManager = userManager;
        _activityLogger = activityLogger;
    }

    // POST: /Reviews/Create
    // Submit review and star rating for a completed property stay
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int propertyId, int rating, string comment)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var property = await _context.HostProperties.FirstOrDefaultAsync(p => p.Id == propertyId && p.IsActive);
        if (property == null) return NotFound();

        // 1. Validation: Rating must be 1 to 5
        if (rating < 1 || rating > 5)
        {
            TempData["ErrorMessage"] = "Please select a star rating between 1 and 5.";
            return RedirectToAction("Details", "Properties", new { id = propertyId });
        }

        // 2. Validation: Comment length
        if (string.IsNullOrWhiteSpace(comment) || comment.Trim().Length < 5)
        {
            TempData["ErrorMessage"] = "Review comment must be at least 5 characters.";
            return RedirectToAction("Details", "Properties", new { id = propertyId });
        }

        // 3. Authorization check: Guest must have an approved or completed reservation
        bool hasConfirmedStay = await _context.Bookings.AnyAsync(b =>
            b.PropertyId == propertyId &&
            b.GuestId == user.Id &&
            (b.Status == BookingStatus.Approved || b.Status == BookingStatus.Completed));

        if (!hasConfirmedStay)
        {
            TempData["ErrorMessage"] = "Only guests with confirmed reservations can leave a review.";
            return RedirectToAction("Details", "Properties", new { id = propertyId });
        }

        // 4. Duplicate prevention
        bool alreadyReviewed = await _context.Reviews.AnyAsync(r =>
            r.PropertyId == propertyId && r.ReviewerId == user.Id);

        if (alreadyReviewed)
        {
            TempData["ErrorMessage"] = "You have already submitted a review for this property.";
            return RedirectToAction("Details", "Properties", new { id = propertyId });
        }

        var review = new Review
        {
            PropertyId = propertyId,
            ReviewerId = user.Id,
            Rating = rating,
            Comment = comment.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);

        // Audit log
        await _activityLogger.LogAsync(
            user.Id,
            "Review Submitted",
            ActivityType.BookingUpdate,
            $"Guest submitted a {rating}-star review for '{property.Title}'.");

        // Notify Host
        _context.Notifications.Add(new Notification
        {
            UserId = property.HostId,
            Title = "New Property Review",
            Message = $"{user.FullName} rated '{property.Title}' {rating} stars: \"{(comment.Length > 50 ? comment.Substring(0, 47) + "..." : comment)}\"",
            Type = NotificationType.SystemAlert,
            TargetUrl = $"/Properties/Details/{propertyId}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Thank you! Your review has been published.";
        return RedirectToAction("Details", "Properties", new { id = propertyId });
    }
}
