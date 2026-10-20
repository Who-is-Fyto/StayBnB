using Stay_BnB.Models.Enums;

namespace Stay_BnB.Services;

public interface IActivityLogger
{
    Task LogAsync(string? userId, string action, ActivityType type, string? details = null);
}
