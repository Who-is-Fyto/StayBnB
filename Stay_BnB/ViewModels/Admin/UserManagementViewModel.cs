using Stay_BnB.Models;

namespace Stay_BnB.ViewModels.Admin;

public class UserItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Guest";
    public DateTime CreatedAt { get; set; }
    public int PropertiesCount { get; set; }
    public int BookingsCount { get; set; }
    public bool CanBePromotedToAdmin => Role.Equals("Guest", StringComparison.OrdinalIgnoreCase);
}

public class UserManagementViewModel
{
    public List<UserItemViewModel> Users { get; set; } = new List<UserItemViewModel>();
    public string? SearchTerm { get; set; }
    public string? RoleFilter { get; set; }

    public int TotalUsers => Users.Count;
    public int GuestCount { get; set; }
    public int HostCount { get; set; }
    public int AdminCount { get; set; }
}
