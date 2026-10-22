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
using Stay_BnB.ViewModels.Host;

namespace Stay_BnB.Controllers;

[Authorize(Roles = "Host")]
public class HostController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogger _activityLogger;
    private readonly IFileService _fileService;

    public HostController(
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

    // GET: /Host/Index
    // Host dashboard with revenue, property counts, and pending reservations
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var properties = await _context.HostProperties
            .Include(p => p.Images)
            .Include(p => p.Bookings)
            .Where(p => p.HostId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var bookings = await _context.Bookings
            .Include(b => b.Property)
            .Include(b => b.Guest)
            .Include(b => b.Payment)
            .Where(b => b.Property != null && b.Property.HostId == user.Id)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var approvedOrCompleted = bookings
            .Where(b => b.Status == BookingStatus.Approved || b.Status == BookingStatus.Completed)
            .ToList();

        var pendingBookings = bookings
            .Where(b => b.Status == BookingStatus.Pending)
            .ToList();

        var model = new HostDashboardViewModel
        {
            HostName = user.FullName,
            TotalListings = properties.Count,
            ActiveListings = properties.Count(p => p.IsActive),
            InactiveListings = properties.Count(p => !p.IsActive),
            TotalEarnings = approvedOrCompleted.Sum(b => b.TotalPrice),
            TotalReservations = bookings.Count,
            PendingReservationsCount = pendingBookings.Count,
            PendingReservations = pendingBookings,
            RecentListings = properties.Take(5).ToList()
        };

        return View(model);
    }

    // GET: /Host/Listings
    // All listings owned by the host with status and management actions
    [HttpGet]
    public async Task<IActionResult> Listings()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var properties = await _context.HostProperties
            .Include(p => p.Images)
            .Include(p => p.Bookings)
            .Include(p => p.Reviews)
            .Where(p => p.HostId == user.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var model = new HostListingsViewModel
        {
            Properties = properties
        };

        return View(model);
    }

    // GET: /Host/CreateListing
    // Form to create an additional property listing
    [HttpGet]
    public async Task<IActionResult> CreateListing()
    {
        var amenities = await _context.Amenities.OrderBy(a => a.Name).ToListAsync();
        var model = new CreateListingViewModel
        {
            AvailableAmenities = amenities
        };
        return View(model);
    }

    // POST: /Host/CreateListing
    // Submit new property listing for an approved host
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateListing(CreateListingViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        // Enforce mandatory photo upload rule
        if (model.Photos == null || model.Photos.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Photos), "At least one property image is required.");
        }

        if (!ModelState.IsValid)
        {
            model.AvailableAmenities = await _context.Amenities.OrderBy(a => a.Name).ToListAsync();
            return View(model);
        }

        var property = new HostProperty
        {
            HostId = user.Id,
            Title = model.Title,
            Description = model.Description,
            Address = model.Address,
            City = model.City,
            PropertyType = model.PropertyType,
            PricePerNight = model.PricePerNight,
            CleaningFee = model.CleaningFee,
            ServiceFee = model.ServiceFee,
            MaxGuests = model.MaxGuests,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // Attach amenities
        if (model.SelectedAmenityIds.Any())
        {
            var amenities = await _context.Amenities
                .Where(a => model.SelectedAmenityIds.Contains(a.Id))
                .ToListAsync();
            property.Amenities = amenities;
        }

        // Save uploaded photos
        bool isFirst = true;
        foreach (var photo in model.Photos!)
        {
            if (photo.Length > 0)
            {
                var imagePath = await _fileService.SavePropertyImageAsync(photo);
                property.Images.Add(new PropertyImage
                {
                    ImageUrl = imagePath,
                    IsCover = isFirst
                });
                isFirst = false;
            }
        }

        // Initialize check-in procedure
        property.CheckInProcess = new CheckInProcess
        {
            Title = model.CheckInTitle,
            StepsJson = model.CheckInInstructions,
            CreatedAt = DateTime.UtcNow
        };

        _context.HostProperties.Add(property);
        await _context.SaveChangesAsync();

        // Audit log
        await _activityLogger.LogAsync(
            user.Id,
            "Property Created",
            ActivityType.PropertyCreated,
            $"Host added new listing '{property.Title}' in {property.City}.");

        TempData["SuccessMessage"] = $"Listing '{property.Title}' was published successfully.";
        return RedirectToAction(nameof(Listings));
    }

    // GET: /Host/EditListing/5
    // Form to modify listing details and amenities
    [HttpGet]
    public async Task<IActionResult> EditListing(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var property = await _context.HostProperties
            .Include(p => p.Images)
            .Include(p => p.Amenities)
            .FirstOrDefaultAsync(p => p.Id == id && p.HostId == user.Id);

        if (property == null) return NotFound();

        var allAmenities = await _context.Amenities.OrderBy(a => a.Name).ToListAsync();

        var model = new EditListingViewModel
        {
            Id = property.Id,
            Title = property.Title,
            Description = property.Description,
            PropertyType = property.PropertyType,
            Address = property.Address,
            City = property.City,
            PricePerNight = property.PricePerNight,
            CleaningFee = property.CleaningFee,
            ServiceFee = property.ServiceFee,
            MaxGuests = property.MaxGuests,
            IsActive = property.IsActive,
            SelectedAmenityIds = property.Amenities.Select(a => a.Id).ToList(),
            AvailableAmenities = allAmenities,
            Images = property.Images.ToList()
        };

        return View(model);
    }

    // POST: /Host/EditListing/5
    // Update property details and amenities
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditListing(EditListingViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var property = await _context.HostProperties
            .Include(p => p.Amenities)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == model.Id && p.HostId == user.Id);

        if (property == null) return NotFound();

        if (!ModelState.IsValid)
        {
            model.AvailableAmenities = await _context.Amenities.OrderBy(a => a.Name).ToListAsync();
            model.Images = property.Images.ToList();
            return View(model);
        }

        property.Title = model.Title;
        property.Description = model.Description;
        property.PropertyType = model.PropertyType;
        property.Address = model.Address;
        property.City = model.City;
        property.PricePerNight = model.PricePerNight;
        property.CleaningFee = model.CleaningFee;
        property.ServiceFee = model.ServiceFee;
        property.MaxGuests = model.MaxGuests;
        property.IsActive = model.IsActive;

        // Synchronize amenities
        property.Amenities.Clear();
        if (model.SelectedAmenityIds.Any())
        {
            var amenities = await _context.Amenities
                .Where(a => model.SelectedAmenityIds.Contains(a.Id))
                .ToListAsync();
            foreach (var am in amenities)
            {
                property.Amenities.Add(am);
            }
        }

        await _context.SaveChangesAsync();

        // Audit log
        await _activityLogger.LogAsync(
            user.Id,
            "Property Updated",
            ActivityType.PropertyCreated,
            $"Host updated details for listing '{property.Title}'.");

        TempData["SuccessMessage"] = $"Listing '{property.Title}' updated successfully.";
        return RedirectToAction(nameof(Listings));
    }

    // POST: /Host/ToggleActive/5
    // Quick toggle for property marketplace visibility
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var property = await _context.HostProperties
            .FirstOrDefaultAsync(p => p.Id == id && p.HostId == user.Id);

        if (property == null) return NotFound();

        property.IsActive = !property.IsActive;
        await _context.SaveChangesAsync();

        // Audit log
        await _activityLogger.LogAsync(
            user.Id,
            "Property Visibility Toggled",
            ActivityType.PropertyCreated,
            $"Host set '{property.Title}' visibility to {(property.IsActive ? "Active" : "Inactive")}.");

        TempData["SuccessMessage"] = $"Listing '{property.Title}' is now {(property.IsActive ? "active in search results" : "hidden from search results")}.";
        return RedirectToAction(nameof(Listings));
    }

    // GET: /Host/ManageImages/5
    // Image gallery management for a property
    [HttpGet]
    public async Task<IActionResult> ManageImages(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var property = await _context.HostProperties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id && p.HostId == user.Id);

        if (property == null) return NotFound();

        var model = new ManageImagesViewModel
        {
            PropertyId = property.Id,
            PropertyTitle = property.Title,
            Images = property.Images.OrderByDescending(i => i.IsCover).ThenBy(i => i.Id).ToList()
        };

        return View(model);
    }

    // POST: /Host/AddImages
    // Upload additional photos to listing
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddImages(int propertyId, List<IFormFile> photos)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var property = await _context.HostProperties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.HostId == user.Id);

        if (property == null) return NotFound();

        if (photos == null || photos.Count == 0)
        {
            TempData["ErrorMessage"] = "Please select at least one photo to upload.";
            return RedirectToAction(nameof(ManageImages), new { id = propertyId });
        }

        bool hasCover = property.Images.Any(i => i.IsCover);
        int uploadedCount = 0;

        foreach (var photo in photos)
        {
            if (photo.Length > 0)
            {
                var imagePath = await _fileService.SavePropertyImageAsync(photo);
                property.Images.Add(new PropertyImage
                {
                    ImageUrl = imagePath,
                    IsCover = !hasCover && uploadedCount == 0
                });
                uploadedCount++;
            }
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"{uploadedCount} photo(s) added successfully.";
        return RedirectToAction(nameof(ManageImages), new { id = propertyId });
    }

    // POST: /Host/SetCoverImage
    // Set selected image as the primary cover photo
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCoverImage(int propertyId, int imageId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var property = await _context.HostProperties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.HostId == user.Id);

        if (property == null) return NotFound();

        foreach (var img in property.Images)
        {
            img.IsCover = (img.Id == imageId);
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Cover photo updated.";
        return RedirectToAction(nameof(ManageImages), new { id = propertyId });
    }

    // POST: /Host/DeleteImage
    // Remove an image while enforcing minimum photo rule
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteImage(int propertyId, int imageId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var property = await _context.HostProperties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.HostId == user.Id);

        if (property == null) return NotFound();

        if (property.Images.Count <= 1)
        {
            TempData["ErrorMessage"] = "Cannot delete: listings must have at least one image.";
            return RedirectToAction(nameof(ManageImages), new { id = propertyId });
        }

        var image = property.Images.FirstOrDefault(i => i.Id == imageId);
        if (image == null) return NotFound();

        bool wasCover = image.IsCover;
        _context.PropertyImages.Remove(image);
        _fileService.DeleteFile(image.ImageUrl);

        if (wasCover)
        {
            var nextImage = property.Images.FirstOrDefault(i => i.Id != imageId);
            if (nextImage != null)
            {
                nextImage.IsCover = true;
            }
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Photo deleted successfully.";
        return RedirectToAction(nameof(ManageImages), new { id = propertyId });
    }

    // GET: /Host/Reservations
    // Filterable reservation center for incoming booking requests
    [HttpGet]
    public async Task<IActionResult> Reservations(string? status)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var allReservations = await _context.Bookings
            .Include(b => b.Guest)
            .Include(b => b.Property)
                .ThenInclude(p => p!.Images)
            .Include(b => b.Payment)
            .Include(b => b.GuestCheckIn)
                .ThenInclude(c => c!.GuestDocuments)
            .Where(b => b.Property != null && b.Property.HostId == user.Id)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        var query = allReservations.AsQueryable();
        var currentFilter = string.IsNullOrWhiteSpace(status) ? "All" : status;

        if (!currentFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (Enum.TryParse<BookingStatus>(currentFilter, true, out var parsedStatus))
            {
                query = query.Where(b => b.Status == parsedStatus);
            }
        }

        var model = new HostReservationsViewModel
        {
            Reservations = query.ToList(),
            CurrentStatusFilter = currentFilter,
            TotalCount = allReservations.Count,
            PendingCount = allReservations.Count(b => b.Status == BookingStatus.Pending),
            ApprovedCount = allReservations.Count(b => b.Status == BookingStatus.Approved),
            CompletedCount = allReservations.Count(b => b.Status == BookingStatus.Completed),
            CancelledCount = allReservations.Count(b => b.Status == BookingStatus.Cancelled),
            RejectedCount = allReservations.Count(b => b.Status == BookingStatus.Rejected)
        };

        return View(model);
    }

    // POST: /Host/ApproveReservation/5
    // Host approves pending booking request: updates status, completes payment, and notifies guest
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveReservation(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var booking = await _context.Bookings
            .Include(b => b.Property)
                .ThenInclude(p => p!.CheckInProcess)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == id && b.Property != null && b.Property.HostId == user.Id);

        if (booking == null) return NotFound();

        if (booking.Status != BookingStatus.Pending)
        {
            TempData["ErrorMessage"] = $"Booking #{booking.Id} is not in pending status.";
            return RedirectToAction(nameof(Reservations));
        }

        // Double-booking check: ensure no other approved booking overlaps these dates
        var hasConflict = await _context.Bookings.AnyAsync(b =>
            b.PropertyId == booking.PropertyId &&
            b.Id != booking.Id &&
            b.Status == BookingStatus.Approved &&
            b.CheckInDate < booking.CheckOutDate &&
            b.CheckOutDate > booking.CheckInDate);

        if (hasConflict)
        {
            TempData["ErrorMessage"] = "Cannot approve: another confirmed reservation overlaps with these dates.";
            return RedirectToAction(nameof(Reservations));
        }

        // Update status
        booking.Status = BookingStatus.Approved;
        if (booking.Payment != null)
        {
            booking.Payment.Status = PaymentStatus.Completed;
        }

        // Initialize GuestCheckIn record for Phase 13 workflow
        var existingCheckIn = await _context.GuestCheckIns.FirstOrDefaultAsync(c => c.BookingId == booking.Id);
        if (existingCheckIn == null)
        {
            _context.GuestCheckIns.Add(new GuestCheckIn
            {
                BookingId = booking.Id,
                CheckInProcessId = booking.Property?.CheckInProcess?.Id,
                Status = CheckInStatus.Pending,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Audit log
        await _activityLogger.LogAsync(
            user.Id,
            "Booking Approved",
            ActivityType.BookingUpdate,
            $"Host approved reservation #{booking.Id} for '{booking.Property?.Title}' ({booking.CheckInDate:yyyy-MM-dd} to {booking.CheckOutDate:yyyy-MM-dd}).");

        // Dispatch notification to guest
        _context.Notifications.Add(new Notification
        {
            UserId = booking.GuestId,
            Title = "Reservation Approved",
            Message = $"Your booking request for '{booking.Property?.Title}' has been approved by the host.",
            Type = NotificationType.BookingApproved,
            TargetUrl = $"/Booking/Confirmation/{booking.Id}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Reservation #{booking.Id} has been approved.";
        return RedirectToAction(nameof(Reservations));
    }

    // POST: /Host/RejectReservation/5
    // Host rejects pending booking request: updates status, refunds payment, and notifies guest
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectReservation(int id, string? reason)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var booking = await _context.Bookings
            .Include(b => b.Property)
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == id && b.Property != null && b.Property.HostId == user.Id);

        if (booking == null) return NotFound();

        if (booking.Status != BookingStatus.Pending)
        {
            TempData["ErrorMessage"] = $"Booking #{booking.Id} is not in pending status.";
            return RedirectToAction(nameof(Reservations));
        }

        // Update status
        booking.Status = BookingStatus.Rejected;
        if (booking.Payment != null)
        {
            booking.Payment.Status = PaymentStatus.Refunded;
        }

        // Audit log
        await _activityLogger.LogAsync(
            user.Id,
            "Booking Rejected",
            ActivityType.BookingUpdate,
            $"Host declined reservation #{booking.Id} for '{booking.Property?.Title}'. Reason: {reason ?? "None provided"}");

        // Dispatch notification to guest
        _context.Notifications.Add(new Notification
        {
            UserId = booking.GuestId,
            Title = "Reservation Declined",
            Message = $"Your booking request for '{booking.Property?.Title}' was declined by the host. {(string.IsNullOrWhiteSpace(reason) ? "" : $"Reason: {reason}")}",
            Type = NotificationType.BookingRejected,
            TargetUrl = "/Booking/MyTrips",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        TempData["InfoMessage"] = $"Reservation #{booking.Id} was declined.";
        return RedirectToAction(nameof(Reservations));
    }

    // GET: /Host/ConfigureCheckIn/5
    // Form to configure check-in instructions, key codes, and wifi
    [HttpGet]
    public async Task<IActionResult> ConfigureCheckIn(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var property = await _context.HostProperties
            .Include(p => p.CheckInProcess)
            .FirstOrDefaultAsync(p => p.Id == id && p.HostId == user.Id);

        if (property == null) return NotFound();

        CheckInStepsData steps;
        if (property.CheckInProcess != null && !string.IsNullOrWhiteSpace(property.CheckInProcess.StepsJson))
        {
            try
            {
                steps = JsonSerializer.Deserialize<CheckInStepsData>(property.CheckInProcess.StepsJson) ?? new CheckInStepsData();
            }
            catch
            {
                steps = new CheckInStepsData { HouseRules = property.CheckInProcess.StepsJson };
            }
        }
        else
        {
            steps = new CheckInStepsData();
        }

        var model = new ConfigureCheckInViewModel
        {
            PropertyId = property.Id,
            PropertyTitle = property.Title,
            Title = property.CheckInProcess?.Title ?? "Self check-in with keypad or lockbox",
            CheckInTime = steps.CheckInTime,
            CheckOutTime = steps.CheckOutTime,
            EntryMethod = steps.EntryMethod,
            EntryCode = steps.EntryCode,
            WifiNetwork = steps.WifiNetwork,
            WifiPassword = steps.WifiPassword,
            ParkingInstructions = steps.ParkingInstructions,
            HouseRules = steps.HouseRules,
            RequireIdUpload = steps.RequireIdUpload
        };

        return View(model);
    }

    // POST: /Host/ConfigureCheckIn/5
    // Save updated arrival instructions and access codes
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfigureCheckIn(ConfigureCheckInViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var property = await _context.HostProperties
            .Include(p => p.CheckInProcess)
            .FirstOrDefaultAsync(p => p.Id == model.PropertyId && p.HostId == user.Id);

        if (property == null) return NotFound();

        if (!ModelState.IsValid)
        {
            model.PropertyTitle = property.Title;
            return View(model);
        }

        var steps = new CheckInStepsData
        {
            CheckInTime = model.CheckInTime,
            CheckOutTime = model.CheckOutTime,
            EntryMethod = model.EntryMethod,
            EntryCode = model.EntryCode,
            WifiNetwork = model.WifiNetwork,
            WifiPassword = model.WifiPassword,
            ParkingInstructions = model.ParkingInstructions,
            HouseRules = model.HouseRules,
            RequireIdUpload = model.RequireIdUpload
        };

        var stepsJson = JsonSerializer.Serialize(steps);

        if (property.CheckInProcess == null)
        {
            property.CheckInProcess = new CheckInProcess
            {
                PropertyId = property.Id,
                Title = model.Title,
                StepsJson = stepsJson,
                CreatedAt = DateTime.UtcNow
            };
            _context.CheckInProcesses.Add(property.CheckInProcess);
        }
        else
        {
            property.CheckInProcess.Title = model.Title;
            property.CheckInProcess.StepsJson = stepsJson;
        }

        await _context.SaveChangesAsync();

        // Audit log
        await _activityLogger.LogAsync(
            user.Id,
            "Check-In Configured",
            ActivityType.PropertyCreated,
            $"Host updated check-in instructions and entry rules for '{property.Title}'.");

        TempData["SuccessMessage"] = "Check-in procedure and access codes have been saved.";
        return RedirectToAction(nameof(Listings));
    }

    // GET: /Host/VerifyDocuments/5
    // Interface for host to review uploaded guest identification
    [HttpGet]
    public async Task<IActionResult> VerifyDocuments(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var checkIn = await _context.GuestCheckIns
            .Include(c => c.Booking)
                .ThenInclude(b => b!.Guest)
            .Include(c => c.Booking)
                .ThenInclude(b => b!.Property)
            .Include(c => c.GuestDocuments)
            .FirstOrDefaultAsync(c => c.Id == id && c.Booking != null && c.Booking.Property != null && c.Booking.Property.HostId == user.Id);

        if (checkIn == null || checkIn.Booking == null || checkIn.Booking.Property == null)
        {
            return NotFound();
        }

        var model = new HostDocumentVerificationViewModel
        {
            GuestCheckInId = checkIn.Id,
            BookingId = checkIn.BookingId,
            PropertyTitle = checkIn.Booking.Property.Title,
            GuestName = checkIn.Booking.Guest?.FullName ?? "Guest",
            GuestEmail = checkIn.Booking.Guest?.Email ?? string.Empty,
            CheckInDate = checkIn.Booking.CheckInDate,
            CheckOutDate = checkIn.Booking.CheckOutDate,
            CheckInStatus = checkIn.Status,
            Documents = checkIn.GuestDocuments.OrderByDescending(d => d.UploadedAt).ToList()
        };

        return View(model);
    }

    // POST: /Host/ApproveDocument
    // Approve uploaded guest ID/passport
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveDocument(int documentId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var document = await _context.GuestDocuments
            .Include(d => d.GuestCheckIn)
                .ThenInclude(c => c!.Booking)
                    .ThenInclude(b => b!.Property)
            .Include(d => d.GuestCheckIn)
                .ThenInclude(c => c!.GuestDocuments)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null || document.GuestCheckIn?.Booking?.Property == null)
        {
            return NotFound();
        }

        if (document.GuestCheckIn.Booking.Property.HostId != user.Id)
        {
            return Forbid();
        }

        document.Status = DocumentStatus.Verified;

        // If all documents verified, update overall check-in status
        var allVerified = document.GuestCheckIn.GuestDocuments
            .Where(d => d.Id != document.Id)
            .All(d => d.Status == DocumentStatus.Verified);

        if (allVerified)
        {
            document.GuestCheckIn.Status = CheckInStatus.Verified;
        }

        // Audit log
        await _activityLogger.LogAsync(
            user.Id,
            "Document Verified",
            ActivityType.SecurityEvent,
            $"Host verified {document.DocumentType} for reservation #{document.GuestCheckIn.BookingId}.");

        // Notify guest
        _context.Notifications.Add(new Notification
        {
            UserId = document.GuestCheckIn.Booking.GuestId,
            Title = "Identity Document Verified",
            Message = $"Your {document.DocumentType} for '{document.GuestCheckIn.Booking.Property.Title}' was approved. Check-in access is ready!",
            Type = NotificationType.CheckInUpdate,
            TargetUrl = $"/CheckIn/Start?bookingId={document.GuestCheckIn.BookingId}&step=4",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Document #{document.Id} approved successfully.";
        return RedirectToAction(nameof(VerifyDocuments), new { id = document.GuestCheckInId });
    }

    // POST: /Host/RejectDocument
    // Reject uploaded guest document and request re-upload
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectDocument(int documentId, string? reason)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var document = await _context.GuestDocuments
            .Include(d => d.GuestCheckIn)
                .ThenInclude(c => c!.Booking)
                    .ThenInclude(b => b!.Property)
            .FirstOrDefaultAsync(d => d.Id == documentId);

        if (document == null || document.GuestCheckIn?.Booking?.Property == null)
        {
            return NotFound();
        }

        if (document.GuestCheckIn.Booking.Property.HostId != user.Id)
        {
            return Forbid();
        }

        document.Status = DocumentStatus.Rejected;
        document.GuestCheckIn.Status = CheckInStatus.InProgress;

        // Audit log
        await _activityLogger.LogAsync(
            user.Id,
            "Document Rejected",
            ActivityType.SecurityEvent,
            $"Host rejected document #{document.Id} for reservation #{document.GuestCheckIn.BookingId}. Reason: {reason ?? "Unclear image"}");

        // Notify guest
        _context.Notifications.Add(new Notification
        {
            UserId = document.GuestCheckIn.Booking.GuestId,
            Title = "Document Requires Re-upload",
            Message = $"Your {document.DocumentType} for '{document.GuestCheckIn.Booking.Property.Title}' was rejected. Reason: {reason ?? "Please re-upload a clear, readable copy."}",
            Type = NotificationType.CheckInUpdate,
            TargetUrl = $"/CheckIn/Start?bookingId={document.GuestCheckIn.BookingId}&step=2",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        TempData["ErrorMessage"] = $"Document #{document.Id} was rejected.";
        return RedirectToAction(nameof(VerifyDocuments), new { id = document.GuestCheckInId });
    }
}
