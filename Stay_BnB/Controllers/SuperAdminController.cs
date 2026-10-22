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

[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogger _activityLogger;

    public SuperAdminController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogger activityLogger)
    {
        _context = context;
        _userManager = userManager;
        _activityLogger = activityLogger;
    }

    // GET: /SuperAdmin/Users
    // SuperAdmin user directory with search and role management
    [HttpGet]
    public async Task<IActionResult> Users(string? search, string? roleFilter)
    {
        var usersQuery = _context.Users
            .Include(u => u.HostProperties)
            .Include(u => u.Bookings)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            usersQuery = usersQuery.Where(u =>
                u.Email!.ToLower().Contains(term) ||
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term));
        }

        var rawUsers = await usersQuery.OrderByDescending(u => u.CreatedAt).ToListAsync();
        var userViewModels = new List<UserItemViewModel>();

        int guests = 0;
        int hosts = 0;
        int admins = 0;

        foreach (var user in rawUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "Guest";

            if (primaryRole == "Guest") guests++;
            else if (primaryRole == "Host") hosts++;
            else if (primaryRole == "Admin" || primaryRole == "SuperAdmin") admins++;

            if (string.IsNullOrWhiteSpace(roleFilter) || roleFilter.Equals("All", StringComparison.OrdinalIgnoreCase) || primaryRole.Equals(roleFilter, StringComparison.OrdinalIgnoreCase))
            {
                userViewModels.Add(new UserItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Role = primaryRole,
                    CreatedAt = user.CreatedAt,
                    PropertiesCount = user.HostProperties.Count,
                    BookingsCount = user.Bookings.Count
                });
            }
        }

        var model = new UserManagementViewModel
        {
            Users = userViewModels,
            SearchTerm = search,
            RoleFilter = roleFilter ?? "All",
            GuestCount = guests,
            HostCount = hosts,
            AdminCount = admins
        };

        return View(model);
    }

    // POST: /SuperAdmin/PromoteToAdmin
    // Requirement 4.2 & 1.2: SuperAdmin promotes a Guest to the Admin role
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PromoteToAdmin(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return NotFound();

        var currentSuperAdmin = await _userManager.GetUserAsync(User);
        var targetUser = await _userManager.FindByIdAsync(userId);

        if (targetUser == null)
        {
            TempData["ErrorMessage"] = "User could not be found.";
            return RedirectToAction(nameof(Users));
        }

        var currentRoles = await _userManager.GetRolesAsync(targetUser);

        // Requirement check: Only existing Guests can be promoted to Admin role
        if (!currentRoles.Contains("Guest"))
        {
            TempData["ErrorMessage"] = $"User '{targetUser.Email}' cannot be promoted. Only users in the Guest role can be elevated to Admin.";
            return RedirectToAction(nameof(Users));
        }

        // Add Admin role and remove Guest role
        await _userManager.AddToRoleAsync(targetUser, "Admin");
        await _userManager.RemoveFromRoleAsync(targetUser, "Guest");

        // Record audit activity log
        await _activityLogger.LogAsync(
            currentSuperAdmin?.Id,
            "Guest Promoted to Admin",
            ActivityType.RoleChange,
            $"SuperAdmin elevated user '{targetUser.Email}' from Guest to Admin role.");

        // Dispatch notification to newly promoted user
        _context.Notifications.Add(new Notification
        {
            UserId = targetUser.Id,
            Title = "Administrator Privileges Granted",
            Message = "You have been promoted to the Admin role by a SuperAdmin. You now have access to the Admin moderation portal.",
            Type = NotificationType.SystemAlert,
            TargetUrl = "/Admin",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"User '{targetUser.FullName}' ({targetUser.Email}) was successfully promoted to Admin.";
        return RedirectToAction(nameof(Users));
    }

    // POST: /SuperAdmin/RevokeAdmin
    // Safety action: Revoke Admin role and revert user to Guest
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeAdmin(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) return NotFound();

        var currentSuperAdmin = await _userManager.GetUserAsync(User);
        var targetUser = await _userManager.FindByIdAsync(userId);

        if (targetUser == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction(nameof(Users));
        }

        // Prevent SuperAdmin from demoting themselves
        if (targetUser.Id == currentSuperAdmin?.Id)
        {
            TempData["ErrorMessage"] = "Safety restriction: SuperAdmin cannot revoke their own root administrator privileges.";
            return RedirectToAction(nameof(Users));
        }

        var currentRoles = await _userManager.GetRolesAsync(targetUser);
        if (currentRoles.Contains("SuperAdmin"))
        {
            TempData["ErrorMessage"] = "Root SuperAdmin accounts cannot be revoked.";
            return RedirectToAction(nameof(Users));
        }

        if (currentRoles.Contains("Admin"))
        {
            await _userManager.RemoveFromRoleAsync(targetUser, "Admin");
            await _userManager.AddToRoleAsync(targetUser, "Guest");

            await _activityLogger.LogAsync(
                currentSuperAdmin?.Id,
                "Admin Privileges Revoked",
                ActivityType.RoleChange,
                $"SuperAdmin revoked Admin role from '{targetUser.Email}'. Reverted to Guest.");

            _context.Notifications.Add(new Notification
            {
                UserId = targetUser.Id,
                Title = "Role Update",
                Message = "Your administrative privileges have been revoked by a SuperAdmin. Your account is now in Guest mode.",
                Type = NotificationType.SystemAlert,
                TargetUrl = "/Account/Profile",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Admin privileges revoked for '{targetUser.Email}'.";
        }

        return RedirectToAction(nameof(Users));
    }
}
