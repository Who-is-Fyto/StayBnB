using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.Models.Enums;

namespace Stay_BnB.Services;

public class ActivityLogger : IActivityLogger
{
    private readonly ApplicationDbContext _context;

    public ActivityLogger(ApplicationDbContext context)
    {
        _context = context;
    }

    // Save activity log entry to database
    public async Task LogAsync(string? userId, string action, ActivityType type, string? details = null)
    {
        try
        {
            var log = new ActivityLog
            {
                UserId = userId,
                Action = action,
                ActivityType = type,
                Details = details,
                CreatedAt = DateTime.UtcNow
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Do not break execution if logging encounters an issue
        }
    }
}
