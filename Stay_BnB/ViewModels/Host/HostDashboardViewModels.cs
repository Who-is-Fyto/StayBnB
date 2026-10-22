using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Stay_BnB.Models;

namespace Stay_BnB.ViewModels.Host;

public class HostDashboardViewModel
{
    public string HostName { get; set; } = string.Empty;
    public int TotalListings { get; set; }
    public int ActiveListings { get; set; }
    public int InactiveListings { get; set; }
    public decimal TotalEarnings { get; set; }
    public int TotalReservations { get; set; }
    public int PendingReservationsCount { get; set; }
    public List<Models.Booking> PendingReservations { get; set; } = new List<Models.Booking>();
    public List<HostProperty> RecentListings { get; set; } = new List<HostProperty>();
}

public class HostListingsViewModel
{
    public List<HostProperty> Properties { get; set; } = new List<HostProperty>();
}

public class EditListingViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 100 characters")]
    [Display(Name = "Listing Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(2000, MinimumLength = 20, ErrorMessage = "Description must be at least 20 characters")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Property type is required")]
    [Display(Name = "Property Type")]
    public string PropertyType { get; set; } = "Apartment";

    [Required(ErrorMessage = "Street address is required")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "City / Destination is required")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price per night is required")]
    [Range(10, 10000, ErrorMessage = "Price must be between $10 and $10,000")]
    [Display(Name = "Price per Night ($)")]
    public decimal PricePerNight { get; set; }

    [Range(0, 500, ErrorMessage = "Cleaning fee must be between $0 and $500")]
    [Display(Name = "Cleaning Fee ($)")]
    public decimal CleaningFee { get; set; } = 25.00m;

    [Range(0, 500, ErrorMessage = "Service fee must be between $0 and $500")]
    [Display(Name = "Service Fee ($)")]
    public decimal ServiceFee { get; set; } = 15.00m;

    [Required]
    [Range(1, 30, ErrorMessage = "Guest capacity must be between 1 and 30")]
    [Display(Name = "Maximum Guests")]
    public int MaxGuests { get; set; } = 2;

    [Display(Name = "Active Visibility")]
    public bool IsActive { get; set; }

    public List<int> SelectedAmenityIds { get; set; } = new List<int>();
    public List<Amenity> AvailableAmenities { get; set; } = new List<Amenity>();
    public List<PropertyImage> Images { get; set; } = new List<PropertyImage>();
}

public class CreateListingViewModel
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(100, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 100 characters")]
    [Display(Name = "Listing Title")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(2000, MinimumLength = 20, ErrorMessage = "Description must be at least 20 characters")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Property type is required")]
    [Display(Name = "Property Type")]
    public string PropertyType { get; set; } = "Apartment";

    [Required(ErrorMessage = "Street address is required")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "City / Destination is required")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Price per night is required")]
    [Range(10, 10000, ErrorMessage = "Price must be between $10 and $10,000")]
    [Display(Name = "Price per Night ($)")]
    public decimal PricePerNight { get; set; } = 100.00m;

    [Range(0, 500, ErrorMessage = "Cleaning fee must be between $0 and $500")]
    [Display(Name = "Cleaning Fee ($)")]
    public decimal CleaningFee { get; set; } = 25.00m;

    [Range(0, 500, ErrorMessage = "Service fee must be between $0 and $500")]
    [Display(Name = "Service Fee ($)")]
    public decimal ServiceFee { get; set; } = 15.00m;

    [Required]
    [Range(1, 30, ErrorMessage = "Guest capacity must be between 1 and 30")]
    [Display(Name = "Maximum Guests")]
    public int MaxGuests { get; set; } = 2;

    [Display(Name = "Amenities")]
    public List<int> SelectedAmenityIds { get; set; } = new List<int>();
    public List<Amenity> AvailableAmenities { get; set; } = new List<Amenity>();

    // Arrival instructions
    [Required(ErrorMessage = "Check-in procedure name is required")]
    [Display(Name = "Check-in Procedure Name")]
    public string CheckInTitle { get; set; } = "Self check-in with keypad or lockbox";

    [Required(ErrorMessage = "Arrival instructions are required")]
    [Display(Name = "Arrival Instructions")]
    public string CheckInInstructions { get; set; } = "Keypad code will be provided prior to check-in. Upload ID for entry clearance.";

    // Mandatory property photos
    [Display(Name = "Property Photos")]
    public List<IFormFile> Photos { get; set; } = new List<IFormFile>();
}

public class ManageImagesViewModel
{
    public int PropertyId { get; set; }
    public string PropertyTitle { get; set; } = string.Empty;
    public List<PropertyImage> Images { get; set; } = new List<PropertyImage>();
}

public class HostReservationsViewModel
{
    public List<Models.Booking> Reservations { get; set; } = new List<Models.Booking>();
    public string CurrentStatusFilter { get; set; } = "All";
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int CompletedCount { get; set; }
    public int CancelledCount { get; set; }
    public int RejectedCount { get; set; }
}
