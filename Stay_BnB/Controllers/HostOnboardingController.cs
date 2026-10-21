using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.Models.Enums;
using Stay_BnB.Services;
using Stay_BnB.ViewModels.Host;

namespace Stay_BnB.Controllers;

[Authorize]
public class HostOnboardingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IFileService _fileService;
    private readonly IActivityLogger _activityLogger;

    public HostOnboardingController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IFileService fileService,
        IActivityLogger activityLogger)
    {
        _context = context;
        _userManager = userManager;
        _fileService = fileService;
        _activityLogger = activityLogger;
    }

    // GET: /HostOnboarding/Apply
    [HttpGet]
    public async Task<IActionResult> Apply()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var roles = await _userManager.GetRolesAsync(user);

        // Role check: If user is already a Host, redirect to host dashboard
        if (roles.Contains("Host"))
        {
            return RedirectToAction("Dashboard", "Host");
        }

        // If user is Admin/SuperAdmin, explain role exclusivity
        if (roles.Contains("Admin") || roles.Contains("SuperAdmin"))
        {
            TempData["InfoMessage"] = "Administrators oversee the platform and cannot submit hosting applications.";
            return RedirectToAction("Index", "Home");
        }

        // Check if user already has a pending application
        var pendingApp = await _context.HostApplications
            .Include(a => a.Property)
            .FirstOrDefaultAsync(a => a.ApplicationUserId == user.Id && a.Status == ApplicationStatus.Pending);

        if (pendingApp != null)
        {
            return RedirectToAction(nameof(Status));
        }

        var model = new BecomeHostViewModel
        {
            AvailableAmenities = await _context.Amenities.OrderBy(a => a.Name).ToListAsync()
        };

        return View(model);
    }

    // POST: /HostOnboarding/Apply
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Apply(BecomeHostViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // Enforce requirement: Application cannot be submitted without at least one property image
        if (model.PropertyImages == null || !model.PropertyImages.Any(f => f != null && f.Length > 0))
        {
            ModelState.AddModelError(nameof(model.PropertyImages), "At least one property photo is required. Applications without images cannot be submitted.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableAmenities = await _context.Amenities.OrderBy(a => a.Name).ToListAsync();
            return View(model);
        }

        // 1. Create HostProperty (inactive pending admin approval)
        var property = new HostProperty
        {
            HostId = user.Id,
            Title = model.Title.Trim(),
            Description = model.Description.Trim(),
            PropertyType = model.PropertyType,
            Address = model.Address.Trim(),
            City = model.City.Trim(),
            PricePerNight = model.PricePerNight,
            CleaningFee = model.CleaningFee,
            ServiceFee = model.ServiceFee,
            MaxGuests = model.MaxGuests,
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        // 2. Link selected amenities
        if (model.SelectedAmenityIds != null && model.SelectedAmenityIds.Any())
        {
            var selectedAmenities = await _context.Amenities
                .Where(a => model.SelectedAmenityIds.Contains(a.Id))
                .ToListAsync();

            foreach (var amenity in selectedAmenities)
            {
                property.Amenities.Add(amenity);
            }
        }

        // 3. Save uploaded property images
        bool isFirst = true;
        foreach (var file in model.PropertyImages!)
        {
            if (file != null && file.Length > 0)
            {
                var relativePath = await _fileService.SavePropertyImageAsync(file);
                property.Images.Add(new PropertyImage
                {
                    ImageUrl = relativePath,
                    IsCover = isFirst
                });
                isFirst = false;
            }
        }

        // 4. Create initial CheckInProcess
        property.CheckInProcess = new CheckInProcess
        {
            Title = model.CheckInTitle.Trim(),
            StepsJson = $"[{{\"step\":1,\"instruction\":\"{model.CheckInInstructions.Trim()}\"}}]",
            CreatedAt = DateTime.UtcNow
        };

        _context.HostProperties.Add(property);
        await _context.SaveChangesAsync();

        // 5. Create HostApplication
        var application = new HostApplication
        {
            ApplicationUserId = user.Id,
            PropertyId = property.Id,
            Status = ApplicationStatus.Pending,
            AppliedAt = DateTime.UtcNow
        };

        _context.HostApplications.Add(application);

        // 6. Record activity audit log
        await _activityLogger.LogAsync(
            user.Id,
            "Host Application Submitted",
            ActivityType.HostApplication,
            $"Guest submitted application for '{property.Title}' ({property.City}).");

        // 7. Dispatch user notification
        _context.Notifications.Add(new Notification
        {
            UserId = user.Id,
            Title = "Host Application Received",
            Message = $"Your hosting application for '{property.Title}' is currently under review by our moderation team.",
            Type = NotificationType.ApplicationStatus,
            TargetUrl = "/HostOnboarding/Status",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your hosting application has been submitted successfully!";
        return RedirectToAction(nameof(Status));
    }

    // GET: /HostOnboarding/Status
    [HttpGet]
    public async Task<IActionResult> Status()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var application = await _context.HostApplications
            .Include(a => a.Property)
                .ThenInclude(p => p!.Images)
            .OrderByDescending(a => a.AppliedAt)
            .FirstOrDefaultAsync(a => a.ApplicationUserId == user.Id);

        if (application == null)
        {
            return RedirectToAction(nameof(Apply));
        }

        var viewModel = new ApplicationStatusViewModel
        {
            Application = application,
            Property = application.Property ?? new HostProperty()
        };

        return View(viewModel);
    }
}
