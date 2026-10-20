using System.ComponentModel.DataAnnotations;

namespace Stay_BnB.ViewModels.Account;

public class ProfileViewModel
{
    [Required]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? ProfilePictureUrl { get; set; }

    public string CurrentRole { get; set; } = "Guest";

    public DateTime CreatedAt { get; set; }
}
