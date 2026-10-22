using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.Models.Enums;
using Stay_BnB.ViewModels.Communication;

namespace Stay_BnB.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public NotificationsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Notifications
    // Notification center displaying real-time platform alerts
    [HttpGet]
    public async Task<IActionResult> Index(string? filter)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var allNotifications = await _context.Notifications
            .Where(n => n.UserId == user.Id)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        var currentFilter = string.IsNullOrWhiteSpace(filter) ? "All" : filter;
        var query = allNotifications.AsQueryable();

        if (currentFilter.Equals("Unread", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(n => !n.IsRead);
        }
        else if (currentFilter.Equals("Bookings", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(n => n.Type == NotificationType.BookingApproved ||
                                     n.Type == NotificationType.BookingRejected ||
                                     n.Type == NotificationType.NewBookingRequest);
        }
        else if (currentFilter.Equals("Messages", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(n => n.Type == NotificationType.NewMessage);
        }
        else if (currentFilter.Equals("CheckIn", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(n => n.Type == NotificationType.CheckInUpdate);
        }

        var model = new NotificationsViewModel
        {
            Notifications = query.ToList(),
            CurrentFilter = currentFilter,
            TotalCount = allNotifications.Count,
            UnreadCount = allNotifications.Count(n => !n.IsRead)
        };

        return View(model);
    }

    // POST: /Notifications/MarkAsRead/5
    // Mark a single notification as read and redirect to target
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);

        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }

        if (notification != null && !string.IsNullOrWhiteSpace(notification.TargetUrl))
        {
            return LocalRedirect(notification.TargetUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /Notifications/MarkAllAsRead
    // Bulk mark all unread notifications as read
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == user.Id && !n.IsRead)
            .ToListAsync();

        if (unreadNotifications.Any())
        {
            foreach (var n in unreadNotifications)
            {
                n.IsRead = true;
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "All notifications have been marked as read.";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: /Notifications/Delete/5
    // Remove a notification from user history
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == user.Id);

        if (notification != null)
        {
            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Notification deleted.";
        }

        return RedirectToAction(nameof(Index));
    }
}
