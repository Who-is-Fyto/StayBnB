using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.Models.Enums;
using Stay_BnB.Services;
using Stay_BnB.ViewModels.CheckIn;

namespace Stay_BnB.Controllers;

[Authorize(Roles = "Guest")]
public class CheckInController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogger _activityLogger;
    private readonly IFileService _fileService;

    public CheckInController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogger activityLogger,
        IFileService fileService)
    {
        _context = context;
        _userManager = userManager;
        _activityLogger = activityLogger;
        _fileService = fileService;
    }

    // GET: /CheckIn/Start?bookingId=5&step=1
    // Multi-step digital check-in wizard for confirmed guest bookings
    [HttpGet]
    public async Task<IActionResult> Start(int bookingId, int step = 1)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var booking = await _context.Bookings
            .Include(b => b.Property)
                .ThenInclude(p => p!.Host)
            .Include(b => b.Property)
                .ThenInclude(p => p!.CheckInProcess)
            .Include(b => b.GuestCheckIn)
                .ThenInclude(c => c!.GuestDocuments)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.GuestId == user.Id);

        if (booking == null || booking.Property == null)
        {
            return NotFound();
        }

        // Only confirmed bookings can access digital check-in
        if (booking.Status != BookingStatus.Approved && booking.Status != BookingStatus.Completed)
        {
            TempData["ErrorMessage"] = "Digital check-in is only available for confirmed reservations.";
            return RedirectToAction("MyTrips", "Booking");
        }

        // Ensure GuestCheckIn record exists
        var checkIn = booking.GuestCheckIn;
        if (checkIn == null)
        {
            checkIn = new GuestCheckIn
            {
                BookingId = booking.Id,
                CheckInProcessId = booking.Property.CheckInProcess?.Id,
                Status = CheckInStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            _context.GuestCheckIns.Add(checkIn);
            await _context.SaveChangesAsync();
        }

        // Parse host check-in steps JSON
        CheckInStepsData steps;
        if (booking.Property.CheckInProcess != null && !string.IsNullOrWhiteSpace(booking.Property.CheckInProcess.StepsJson))
        {
            try
            {
                steps = JsonSerializer.Deserialize<CheckInStepsData>(booking.Property.CheckInProcess.StepsJson) ?? new CheckInStepsData();
            }
            catch
            {
                steps = new CheckInStepsData { HouseRules = booking.Property.CheckInProcess.StepsJson };
            }
        }
        else
        {
            steps = new CheckInStepsData();
        }

        // Determine default step based on current verification progress
        int activeStep = step;
        var hasUploadedDocs = checkIn.GuestDocuments.Any();
        var hasVerifiedDocs = checkIn.GuestDocuments.Any(d => d.Status == DocumentStatus.Verified);

        if (checkIn.Status == CheckInStatus.Verified || hasVerifiedDocs)
        {
            if (step <= 3 && !Request.Query.ContainsKey("step"))
            {
                activeStep = 4; // Skip directly to access codes if already verified
            }
        }
        else if (hasUploadedDocs && !Request.Query.ContainsKey("step"))
        {
            activeStep = 3; // Show verification status if waiting for host
        }

        var model = new GuestCheckInWizardViewModel
        {
            BookingId = booking.Id,
            CheckInId = checkIn.Id,
            PropertyTitle = booking.Property.Title,
            PropertyAddress = booking.Property.Address,
            PropertyCity = booking.Property.City,
            HostName = booking.Property.Host?.FullName ?? "Host",
            HostEmail = booking.Property.Host?.Email ?? string.Empty,
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate,
            GuestCount = booking.GuestCount,
            Status = checkIn.Status,
            Steps = steps,
            Documents = checkIn.GuestDocuments.OrderByDescending(d => d.UploadedAt).ToList(),
            ActiveStep = activeStep
        };

        return View(model);
    }

    // POST: /CheckIn/UploadDocument
    // Upload government ID or passport for host review
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDocument(int bookingId, int checkInId, string documentType, IFormFile documentFile)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var booking = await _context.Bookings
            .Include(b => b.Property)
            .Include(b => b.GuestCheckIn)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.GuestId == user.Id);

        if (booking == null || booking.Property == null)
        {
            return NotFound();
        }

        var checkIn = await _context.GuestCheckIns
            .FirstOrDefaultAsync(c => c.Id == checkInId && c.BookingId == booking.Id);

        if (checkIn == null)
        {
            return NotFound();
        }

        // Validate document file
        if (documentFile == null || documentFile.Length == 0)
        {
            TempData["ErrorMessage"] = "Please select a valid document file to upload.";
            return RedirectToAction(nameof(Start), new { bookingId = booking.Id, step = 2 });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
        var ext = Path.GetExtension(documentFile.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
        {
            TempData["ErrorMessage"] = "Only JPG, PNG, WEBP, or PDF files are supported.";
            return RedirectToAction(nameof(Start), new { bookingId = booking.Id, step = 2 });
        }

        if (documentFile.Length > 10 * 1024 * 1024)
        {
            TempData["ErrorMessage"] = "File size cannot exceed 10MB.";
            return RedirectToAction(nameof(Start), new { bookingId = booking.Id, step = 2 });
        }

        // Save file to uploads/documents
        var savedPath = await _fileService.SaveDocumentAsync(documentFile);

        var guestDoc = new GuestDocument
        {
            GuestCheckInId = checkIn.Id,
            DocumentType = string.IsNullOrWhiteSpace(documentType) ? "National ID / Passport" : documentType,
            FileName = savedPath,
            Status = DocumentStatus.PendingVerification,
            UploadedAt = DateTime.UtcNow
        };

        checkIn.Status = CheckInStatus.InProgress;
        _context.GuestDocuments.Add(guestDoc);

        // Audit log
        await _activityLogger.LogAsync(
            user.Id,
            "Document Uploaded",
            ActivityType.SecurityEvent,
            $"Guest uploaded {guestDoc.DocumentType} for reservation #{booking.Id}.");

        // Dispatch notification to host
        _context.Notifications.Add(new Notification
        {
            UserId = booking.Property.HostId,
            Title = "Guest Document Uploaded",
            Message = $"{user.FullName} uploaded an identification document for reservation #{booking.Id}.",
            Type = NotificationType.CheckInUpdate,
            TargetUrl = $"/Host/VerifyDocuments/{checkIn.Id}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Your document has been uploaded and submitted to the host for verification.";
        return RedirectToAction(nameof(Start), new { bookingId = booking.Id, step = 3 });
    }
}
