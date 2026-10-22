using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.Models.Enums;
using Stay_BnB.Services;
using Stay_BnB.ViewModels.Booking;

namespace Stay_BnB.Controllers;

[Authorize(Roles = "Guest")]
public class BookingController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogger _activityLogger;

    public BookingController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogger activityLogger)
    {
        _context = context;
        _userManager = userManager;
        _activityLogger = activityLogger;
    }

    // GET: /Booking/Create
    // Booking checkout form with price calculator and date selection
    [HttpGet]
    public async Task<IActionResult> Create(int propertyId, DateTime? checkIn, DateTime? checkOut, int? guests)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var property = await _context.HostProperties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.IsActive);

        if (property == null) return NotFound();

        // Prevent host from booking their own listing
        if (property.HostId == user.Id)
        {
            TempData["ErrorMessage"] = "You cannot book your own property listing.";
            return RedirectToAction("Details", "Properties", new { id = propertyId });
        }

        var defaultCheckIn = checkIn ?? DateTime.Today.AddDays(1);
        var defaultCheckOut = checkOut ?? defaultCheckIn.AddDays(3);
        if (defaultCheckOut <= defaultCheckIn)
        {
            defaultCheckOut = defaultCheckIn.AddDays(1);
        }

        var coverImg = property.Images.FirstOrDefault(i => i.IsCover)?.ImageUrl 
            ?? property.Images.FirstOrDefault()?.ImageUrl 
            ?? "https://images.unsplash.com/photo-1512917774080-9991f1c4c750?auto=format&fit=crop&w=800&q=80";

        var model = new CreateBookingViewModel
        {
            PropertyId = property.Id,
            PropertyTitle = property.Title,
            PropertyAddress = property.Address,
            PropertyCity = property.City,
            PropertyType = property.PropertyType,
            CoverImageUrl = coverImg,
            PricePerNight = property.PricePerNight,
            CleaningFee = property.CleaningFee,
            ServiceFee = property.ServiceFee,
            MaxGuests = property.MaxGuests,
            CheckInDate = defaultCheckIn,
            CheckOutDate = defaultCheckOut,
            GuestCount = (guests.HasValue && guests.Value > 0 && guests.Value <= property.MaxGuests) ? guests.Value : 1
        };

        return View(model);
    }

    // POST: /Booking/Create
    // Submit reservation: validates dates, checks double-booking, and calculates exact price
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var property = await _context.HostProperties
            .Include(p => p.Images)
            .Include(p => p.Host)
            .FirstOrDefaultAsync(p => p.Id == model.PropertyId && p.IsActive);

        if (property == null) return NotFound();

        // 1. Date validation
        if (model.CheckInDate < DateTime.Today)
        {
            ModelState.AddModelError(nameof(model.CheckInDate), "Check-in date cannot be in the past.");
        }
        if (model.CheckOutDate <= model.CheckInDate)
        {
            ModelState.AddModelError(nameof(model.CheckOutDate), "Check-out date must be after check-in date.");
        }

        // 2. Guest capacity validation
        if (model.GuestCount > property.MaxGuests)
        {
            ModelState.AddModelError(nameof(model.GuestCount), $"This property accommodates a maximum of {property.MaxGuests} guests.");
        }

        // 3. Prevent double-booking: check for conflicting confirmed bookings
        var hasConflict = await _context.Bookings.AnyAsync(b =>
            b.PropertyId == property.Id &&
            b.Status == BookingStatus.Approved &&
            b.CheckInDate < model.CheckOutDate &&
            b.CheckOutDate > model.CheckInDate);

        if (hasConflict)
        {
            ModelState.AddModelError(string.Empty, "The requested dates are no longer available for this property.");
        }

        if (!ModelState.IsValid)
        {
            model.PropertyTitle = property.Title;
            model.PropertyAddress = property.Address;
            model.PropertyCity = property.City;
            model.PropertyType = property.PropertyType;
            model.PricePerNight = property.PricePerNight;
            model.CleaningFee = property.CleaningFee;
            model.ServiceFee = property.ServiceFee;
            model.MaxGuests = property.MaxGuests;
            model.CoverImageUrl = property.Images.FirstOrDefault(i => i.IsCover)?.ImageUrl 
                ?? property.Images.FirstOrDefault()?.ImageUrl 
                ?? "https://images.unsplash.com/photo-1512917774080-9991f1c4c750?auto=format&fit=crop&w=800&q=80";
            return View(model);
        }

        // 4. Exact pricing calculation: TotalPrice = (PricePerNight x Nights) + CleaningFee + ServiceFee
        int nights = (int)(model.CheckOutDate - model.CheckInDate).TotalDays;
        decimal nightlyTotal = property.PricePerNight * nights;
        decimal calculatedTotal = nightlyTotal + property.CleaningFee + property.ServiceFee;

        // 5. Create Booking record
        var booking = new Models.Booking
        {
            PropertyId = property.Id,
            GuestId = user.Id,
            CheckInDate = model.CheckInDate,
            CheckOutDate = model.CheckOutDate,
            GuestCount = model.GuestCount,
            TotalPrice = calculatedTotal,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        // 6. Create associated Payment record (One-to-One)
        var payment = new Payment
        {
            Amount = calculatedTotal,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        booking.Payment = payment;

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        // 7. Audit log
        await _activityLogger.LogAsync(
            user.Id,
            "New Booking Request",
            ActivityType.BookingUpdate,
            $"Guest requested reservation #{booking.Id} for '{property.Title}' ({nights} nights, ${calculatedTotal}).");

        // 8. Notifications
        // Notify Host of pending request
        _context.Notifications.Add(new Notification
        {
            UserId = property.HostId,
            Title = "New Booking Request",
            Message = $"You received a reservation request from {user.FullName} for '{property.Title}' ({nights} nights).",
            Type = NotificationType.NewBookingRequest,
            TargetUrl = "/Host/Reservations",
            CreatedAt = DateTime.UtcNow
        });

        // Notify Guest of confirmation
        _context.Notifications.Add(new Notification
        {
            UserId = user.Id,
            Title = "Booking Request Sent",
            Message = $"Your reservation request for '{property.Title}' has been sent to the host for approval.",
            Type = NotificationType.SystemAlert,
            TargetUrl = $"/Booking/Confirmation/{booking.Id}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Confirmation), new { id = booking.Id });
    }

    // GET: /Booking/Confirmation/5
    // Receipt and confirmation summary for submitted reservation
    [HttpGet]
    public async Task<IActionResult> Confirmation(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var booking = await _context.Bookings
            .Include(b => b.Payment)
            .Include(b => b.Property)
                .ThenInclude(p => p!.Images)
            .Include(b => b.Property)
                .ThenInclude(p => p!.Host)
            .FirstOrDefaultAsync(b => b.Id == id && b.GuestId == user.Id);

        if (booking == null || booking.Property == null) return NotFound();

        var model = new BookingConfirmationViewModel
        {
            Booking = booking,
            Payment = booking.Payment,
            Property = booking.Property
        };

        return View(model);
    }

    // GET: /Booking/MyTrips
    // Guest dashboard displaying all trips and reservations
    [HttpGet]
    public async Task<IActionResult> MyTrips(string? filter)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var query = _context.Bookings
            .Include(b => b.Payment)
            .Include(b => b.Property)
                .ThenInclude(p => p!.Images)
            .Where(b => b.GuestId == user.Id)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter) && !filter.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (Enum.TryParse<BookingStatus>(filter, true, out var status))
            {
                query = query.Where(b => b.Status == status);
            }
        }

        var bookings = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();

        var model = new MyTripsViewModel
        {
            Bookings = bookings,
            CurrentFilter = filter ?? "All"
        };

        return View(model);
    }

    // POST: /Booking/Cancel/5
    // Cancel a pending or approved booking
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var booking = await _context.Bookings
            .Include(b => b.Property)
            .FirstOrDefaultAsync(b => b.Id == id && b.GuestId == user.Id);

        if (booking == null) return NotFound();

        if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Completed)
        {
            TempData["ErrorMessage"] = "This booking cannot be cancelled.";
            return RedirectToAction(nameof(MyTrips));
        }

        booking.Status = BookingStatus.Cancelled;
        if (booking.Payment != null)
        {
            booking.Payment.Status = PaymentStatus.Refunded;
        }

        // Audit log
        await _activityLogger.LogAsync(
            user.Id,
            "Booking Cancelled",
            ActivityType.BookingUpdate,
            $"Guest cancelled booking #{booking.Id} for '{booking.Property?.Title}'.");

        // Notify Host
        if (booking.Property != null)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = booking.Property.HostId,
                Title = "Booking Cancelled",
                Message = $"The reservation for '{booking.Property.Title}' ({booking.CheckInDate:MMM dd} - {booking.CheckOutDate:MMM dd}) was cancelled by the guest.",
                Type = NotificationType.SystemAlert,
                TargetUrl = "/Host/Reservations",
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Your reservation has been cancelled.";
        return RedirectToAction(nameof(MyTrips));
    }
}
