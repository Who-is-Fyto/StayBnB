using System.ComponentModel.DataAnnotations;
using Stay_BnB.Models;

namespace Stay_BnB.ViewModels.Booking;

public class CreateBookingViewModel
{
    public int PropertyId { get; set; }
    public string PropertyTitle { get; set; } = string.Empty;
    public string PropertyAddress { get; set; } = string.Empty;
    public string PropertyCity { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
    public string CoverImageUrl { get; set; } = string.Empty;
    public decimal PricePerNight { get; set; }
    public decimal CleaningFee { get; set; }
    public decimal ServiceFee { get; set; }
    public int MaxGuests { get; set; }

    [Required(ErrorMessage = "Check-in date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Check-in Date")]
    public DateTime CheckInDate { get; set; }

    [Required(ErrorMessage = "Check-out date is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Check-out Date")]
    public DateTime CheckOutDate { get; set; }

    [Required]
    [Range(1, 30, ErrorMessage = "Guest count must be at least 1")]
    [Display(Name = "Number of Travelers")]
    public int GuestCount { get; set; } = 1;

    [Display(Name = "Special Requests (Optional)")]
    [StringLength(500)]
    public string? SpecialRequests { get; set; }

    // Calculated pricing properties
    public int Nights => (CheckOutDate > CheckInDate) ? (int)(CheckOutDate - CheckInDate).TotalDays : 1;
    public decimal NightlySubtotal => PricePerNight * Nights;
    public decimal TotalPrice => NightlySubtotal + CleaningFee + ServiceFee;
}

public class BookingConfirmationViewModel
{
    public Models.Booking Booking { get; set; } = null!;
    public Payment? Payment { get; set; }
    public HostProperty Property { get; set; } = null!;
}

public class MyTripsViewModel
{
    public List<Models.Booking> Bookings { get; set; } = new List<Models.Booking>();
    public string CurrentFilter { get; set; } = "All";
}
