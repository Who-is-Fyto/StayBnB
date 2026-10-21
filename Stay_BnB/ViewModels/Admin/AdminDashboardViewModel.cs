using Stay_BnB.Models;

namespace Stay_BnB.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public int PendingApplicationsCount { get; set; }
    public int TotalPropertiesCount { get; set; }
    public int ActiveListingsCount { get; set; }
    public int TotalUsersCount { get; set; }

    public List<HostApplication> PendingApplications { get; set; } = new List<HostApplication>();
    public List<ActivityLog> RecentActivities { get; set; } = new List<ActivityLog>();
}
