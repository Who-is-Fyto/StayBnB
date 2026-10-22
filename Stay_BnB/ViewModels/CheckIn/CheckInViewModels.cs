using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Stay_BnB.Models;
using Stay_BnB.Models.Enums;

namespace Stay_BnB.ViewModels.CheckIn;

// Structured data stored inside CheckInProcess.StepsJson
public class CheckInStepsData
{
    public string CheckInTime { get; set; } = "15:00";
    public string CheckOutTime { get; set; } = "11:00";
    public string EntryMethod { get; set; } = "Keypad";
    public string EntryCode { get; set; } = "4829";
    public string WifiNetwork { get; set; } = "Staybnb-Guest";
    public string WifiPassword { get; set; } = "Welcome2Staybnb";
    public string ParkingInstructions { get; set; } = "Designated parking bay #4 on premises.";
    public string HouseRules { get; set; } = "No smoking inside. Quiet hours after 22:00.";
    public bool RequireIdUpload { get; set; } = true;
}

// Host form for configuring arrival procedure and codes
public class ConfigureCheckInViewModel
{
    public int PropertyId { get; set; }
    public string PropertyTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Check-in procedure name is required")]
    [StringLength(200, MinimumLength = 3)]
    [Display(Name = "Check-in Procedure Name")]
    public string Title { get; set; } = "Standard Self Check-in";

    [Required]
    [Display(Name = "Check-in Time")]
    public string CheckInTime { get; set; } = "15:00";

    [Required]
    [Display(Name = "Check-out Time")]
    public string CheckOutTime { get; set; } = "11:00";

    [Required]
    [Display(Name = "Entry Method")]
    public string EntryMethod { get; set; } = "Keypad";

    [Required(ErrorMessage = "Entry access code or instructions are required")]
    [Display(Name = "Keypad / Lockbox Entry Code")]
    public string EntryCode { get; set; } = "4829";

    [Display(Name = "Wi-Fi Network Name (SSID)")]
    public string WifiNetwork { get; set; } = "Staybnb-Guest";

    [Display(Name = "Wi-Fi Password")]
    public string WifiPassword { get; set; } = "Welcome2Staybnb";

    [Display(Name = "Parking & Access Notes")]
    public string ParkingInstructions { get; set; } = "Park in designated bay #4.";

    [Display(Name = "House Rules & Special Guidelines")]
    public string HouseRules { get; set; } = "No smoking indoors. Quiet hours after 22:00.";

    [Display(Name = "Require Guest ID or Passport Upload")]
    public bool RequireIdUpload { get; set; } = true;
}

// Guest multi-step check-in wizard
public class GuestCheckInWizardViewModel
{
    public int BookingId { get; set; }
    public int CheckInId { get; set; }
    public string PropertyTitle { get; set; } = string.Empty;
    public string PropertyAddress { get; set; } = string.Empty;
    public string PropertyCity { get; set; } = string.Empty;
    public string HostName { get; set; } = string.Empty;
    public string HostEmail { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public int GuestCount { get; set; }
    public CheckInStatus Status { get; set; }
    public CheckInStepsData Steps { get; set; } = new CheckInStepsData();
    public List<GuestDocument> Documents { get; set; } = new List<GuestDocument>();
    public int ActiveStep { get; set; } = 1;
}

// Upload government ID or passport
public class UploadDocumentViewModel
{
    [Required]
    public int GuestCheckInId { get; set; }

    [Required(ErrorMessage = "Document type is required")]
    [Display(Name = "Document Type")]
    public string DocumentType { get; set; } = "National ID / Passport";

    [Required(ErrorMessage = "Please select a document file to upload")]
    [Display(Name = "Document File (JPG, PNG, PDF)")]
    public IFormFile? DocumentFile { get; set; }
}

// Host inspection and document verification model
public class HostDocumentVerificationViewModel
{
    public int GuestCheckInId { get; set; }
    public int BookingId { get; set; }
    public string PropertyTitle { get; set; } = string.Empty;
    public string GuestName { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public CheckInStatus CheckInStatus { get; set; }
    public List<GuestDocument> Documents { get; set; } = new List<GuestDocument>();
}
