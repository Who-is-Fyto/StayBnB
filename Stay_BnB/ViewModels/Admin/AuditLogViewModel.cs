using Stay_BnB.Models;

namespace Stay_BnB.ViewModels.Admin;

public class AuditLogViewModel
{
    public List<ActivityLog> Logs { get; set; } = new List<ActivityLog>();
    public string? CurrentSearch { get; set; }
    public string? CurrentActivityType { get; set; }
    public int TotalCount { get; set; }
    public int TodayCount { get; set; }
    public int SecurityCount { get; set; }
    public int RoleChangesCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}
