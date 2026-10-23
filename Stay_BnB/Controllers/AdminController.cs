using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.Models.Enums;
using Stay_BnB.Services;
using Stay_BnB.ViewModels.Admin;

namespace Stay_BnB.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogger _activityLogger;

    public AdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogger activityLogger)
    {
        _context = context;
        _userManager = userManager;
        _activityLogger = activityLogger;
    }

    // GET: /Admin
    // Command center overview with platform metrics and recent activities
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var pendingCount = await _context.HostApplications.CountAsync(a => a.Status == ApplicationStatus.Pending);
        var totalProperties = await _context.HostProperties.CountAsync();
        var activeListings = await _context.HostProperties.CountAsync(p => p.IsActive);
        var totalUsers = await _userManager.Users.CountAsync();

        var pendingApps = await _context.HostApplications
            .Include(a => a.ApplicationUser)
            .Include(a => a.Property)
                .ThenInclude(p => p!.Images)
            .Where(a => a.Status == ApplicationStatus.Pending)
            .OrderByDescending(a => a.AppliedAt)
            .Take(10)
            .ToListAsync();

        var recentActivities = await _context.ActivityLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToListAsync();

        var viewModel = new AdminDashboardViewModel
        {
            PendingApplicationsCount = pendingCount,
            TotalPropertiesCount = totalProperties,
            ActiveListingsCount = activeListings,
            TotalUsersCount = totalUsers,
            PendingApplications = pendingApps,
            RecentActivities = recentActivities
        };

        return View(viewModel);
    }

    // GET: /Admin/Applications
    // Filterable list of all host applications
    [HttpGet]
    public async Task<IActionResult> Applications(string? status)
    {
        var query = _context.HostApplications
            .Include(a => a.ApplicationUser)
            .Include(a => a.Property)
                .ThenInclude(p => p!.Images)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ApplicationStatus>(status, true, out var appStatus))
        {
            query = query.Where(a => a.Status == appStatus);
        }

        var applications = await query.OrderByDescending(a => a.AppliedAt).ToListAsync();
        ViewBag.CurrentFilter = status ?? "All";
        return View(applications);
    }

    // GET: /Admin/Review/5
    // Detailed inspection view for evaluating property listing and applicant
    [HttpGet]
    public async Task<IActionResult> Review(int? id)
    {
        if (id == null) return NotFound();

        var application = await _context.HostApplications
            .Include(a => a.ApplicationUser)
            .Include(a => a.Property)
                .ThenInclude(p => p!.Images)
            .Include(a => a.Property)
                .ThenInclude(p => p!.Amenities)
            .Include(a => a.Property)
                .ThenInclude(p => p!.CheckInProcess)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application == null || application.Property == null || application.ApplicationUser == null)
        {
            return NotFound();
        }

        var viewModel = new ApplicationReviewViewModel
        {
            Application = application,
            Property = application.Property,
            Applicant = application.ApplicationUser
        };

        return View(viewModel);
    }

    // POST: /Admin/Approve/5
    // Approve application: Transition user from Guest to Host and activate listing
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        var adminUser = await _userManager.GetUserAsync(User);
        var application = await _context.HostApplications
            .Include(a => a.ApplicationUser)
            .Include(a => a.Property)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application == null || application.Property == null || application.ApplicationUser == null)
        {
            return NotFound();
        }

        if (application.Status == ApplicationStatus.Approved)
        {
            TempData["InfoMessage"] = "This application has already been approved.";
            return RedirectToAction(nameof(Applications));
        }

        var applicant = application.ApplicationUser;
        var property = application.Property;

        // Requirement 1.3: Programmatically add Host role and remove Guest role
        var currentRoles = await _userManager.GetRolesAsync(applicant);
        if (!currentRoles.Contains("Host"))
        {
            await _userManager.AddToRoleAsync(applicant, "Host");
        }
        if (currentRoles.Contains("Guest"))
        {
            await _userManager.RemoveFromRoleAsync(applicant, "Guest");
        }

        // Update application state
        application.Status = ApplicationStatus.Approved;
        application.ReviewedAt = DateTime.UtcNow;

        // Activate property listing for public search
        property.IsActive = true;

        // Record audit activity log
        await _activityLogger.LogAsync(
            adminUser?.Id,
            "Host Application Approved",
            ActivityType.RoleChange,
            $"Admin approved application #{id}. User '{applicant.Email}' upgraded to Host (Guest role removed). Property '{property.Title}' activated.");

        // Dispatch notification to user
        _context.Notifications.Add(new Notification
        {
            UserId = applicant.Id,
            Title = "Host Application Approved",
            Message = $"Congratulations! Your hosting application for '{property.Title}' has been approved. Your account is now upgraded to Host mode.",
            Type = NotificationType.ApplicationStatus,
            TargetUrl = "/Host/Dashboard",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Application #{id} approved successfully. User '{applicant.FullName}' is now a Host.";
        return RedirectToAction(nameof(Applications));
    }

    // POST: /Admin/Reject/5
    // Reject application with feedback: User remains in Guest role
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id, string? rejectionReason)
    {
        var adminUser = await _userManager.GetUserAsync(User);
        var application = await _context.HostApplications
            .Include(a => a.ApplicationUser)
            .Include(a => a.Property)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application == null || application.Property == null || application.ApplicationUser == null)
        {
            return NotFound();
        }

        var applicant = application.ApplicationUser;
        var property = application.Property;

        // Update application state
        application.Status = ApplicationStatus.Rejected;
        application.RejectionReason = string.IsNullOrWhiteSpace(rejectionReason)
            ? "Your property submission does not meet current marketplace quality standards."
            : rejectionReason.Trim();
        application.ReviewedAt = DateTime.UtcNow;

        // Property remains inactive
        property.IsActive = false;

        // User remains in Guest role (no role change)

        // Record audit activity log
        await _activityLogger.LogAsync(
            adminUser?.Id,
            "Host Application Rejected",
            ActivityType.RoleChange,
            $"Admin rejected application #{id} for '{property.Title}'. Reason: {application.RejectionReason}");

        // Dispatch notification with reason to applicant
        _context.Notifications.Add(new Notification
        {
            UserId = applicant.Id,
            Title = "Host Application Update",
            Message = $"Your hosting application for '{property.Title}' was declined. Feedback: {application.RejectionReason}",
            Type = NotificationType.ApplicationStatus,
            TargetUrl = "/HostOnboarding/Status",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Application #{id} has been rejected with feedback provided to the applicant.";
        return RedirectToAction(nameof(Applications));
    }

    // GET: /Admin/AuditLogs
    // Searchable security audit log viewer for Admins and SuperAdmins
    [HttpGet]
    public async Task<IActionResult> AuditLogs(string? search, string? activityType, int page = 1)
    {
        var query = _context.ActivityLogs
            .Include(a => a.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(a => a.Action.ToLower().Contains(s) ||
                                     (a.Details != null && a.Details.ToLower().Contains(s)) ||
                                     (a.User != null && (a.User.FirstName.ToLower().Contains(s) ||
                                                         a.User.LastName.ToLower().Contains(s) ||
                                                         (a.User.Email != null && a.User.Email.ToLower().Contains(s)))));
        }

        if (!string.IsNullOrWhiteSpace(activityType) && Enum.TryParse<ActivityType>(activityType, true, out var type))
        {
            query = query.Where(a => a.ActivityType == type);
        }

        var totalLogs = await query.CountAsync();
        var today = DateTime.UtcNow.Date;
        var todayCount = await _context.ActivityLogs.CountAsync(a => a.CreatedAt >= today);
        var securityCount = await _context.ActivityLogs.CountAsync(a => a.ActivityType == ActivityType.SecurityEvent);
        var roleChangesCount = await _context.ActivityLogs.CountAsync(a => a.ActivityType == ActivityType.RoleChange);

        int pageSize = 25;
        int totalPages = (int)Math.Ceiling(totalLogs / (double)pageSize);
        if (totalPages < 1) totalPages = 1;
        if (page < 1) page = 1;

        var logs = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var model = new AuditLogViewModel
        {
            Logs = logs,
            CurrentSearch = search,
            CurrentActivityType = activityType,
            TotalCount = totalLogs,
            TodayCount = todayCount,
            SecurityCount = securityCount,
            RoleChangesCount = roleChangesCount,
            CurrentPage = page,
            TotalPages = totalPages
        };

        return View(model);
    }
}
