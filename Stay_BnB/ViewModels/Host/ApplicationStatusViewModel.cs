using Stay_BnB.Models;

namespace Stay_BnB.ViewModels.Host;

public class ApplicationStatusViewModel
{
    public HostApplication Application { get; set; } = null!;
    public HostProperty Property { get; set; } = null!;
    public bool HasPendingApplication => Application != null && Application.Status == Models.Enums.ApplicationStatus.Pending;
}
