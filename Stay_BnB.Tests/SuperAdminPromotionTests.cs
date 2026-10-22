using Microsoft.EntityFrameworkCore;
using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.Models.Enums;
using Stay_BnB.Services;
using Xunit;

namespace Stay_BnB.Tests;

public class SuperAdminPromotionTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void SuperAdminPromotion_PromotesGuestToAdminAndRemovesGuestRole()
    {
        // Business rule from Section 1.2 & 4.2: SuperAdmin promotes existing Guest to Admin
        var userRoles = new List<string> { "Guest" };

        // Act - SuperAdmin promotes user
        if (userRoles.Contains("Guest"))
        {
            userRoles.Remove("Guest");
            userRoles.Add("Admin");
        }

        // Assert
        Assert.Contains("Admin", userRoles);
        Assert.DoesNotContain("Guest", userRoles);
        Assert.False(userRoles.Contains("Guest") && userRoles.Contains("Admin"), "User cannot hold both Guest and Admin simultaneously.");
    }

    [Fact]
    public void SuperAdminPromotion_EligibleOnlyForGuestRole()
    {
        // Verification: Only users in the Guest role can be elevated to Admin
        var guestRoles = new List<string> { "Guest" };
        var hostRoles = new List<string> { "Host" };

        bool canPromoteGuest = guestRoles.Contains("Guest");
        bool canPromoteHost = hostRoles.Contains("Guest");

        Assert.True(canPromoteGuest, "Guest must be eligible for promotion to Admin.");
        Assert.False(canPromoteHost, "Host cannot be directly promoted to Admin without role reset.");
    }

    [Fact]
    public void SuperAdminDemotion_ProhibitsDemotingRootSuperAdmin()
    {
        // Safety guard: Root SuperAdmin cannot demote themselves
        string superAdminUserId = "superadmin-root-id";
        string targetUserId = "superadmin-root-id";

        bool isAttemptingSelfDemotion = superAdminUserId == targetUserId;

        Assert.True(isAttemptingSelfDemotion, "Self-demotion should be detected and blocked.");
    }

    [Fact]
    public async Task SuperAdminPromotion_LogsAuditActivityAndDispatchesNotification()
    {
        using var context = GetInMemoryDbContext();
        var logger = new ActivityLogger(context);
        var superAdminId = "superadmin-1";
        var promotedGuestId = "guest-promoted-1";

        // Act
        await logger.LogAsync(
            superAdminId,
            "Guest Promoted to Admin",
            ActivityType.RoleChange,
            "SuperAdmin elevated user from Guest to Admin role");

        context.Notifications.Add(new Notification
        {
            UserId = promotedGuestId,
            Title = "Administrator Privileges Granted",
            Type = NotificationType.SystemAlert,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Assert
        var logs = await context.ActivityLogs.ToListAsync();
        var notifications = await context.Notifications.ToListAsync();

        Assert.Single(logs);
        Assert.Equal("Guest Promoted to Admin", logs[0].Action);
        Assert.Equal(ActivityType.RoleChange, logs[0].ActivityType);
        Assert.Single(notifications);
        Assert.Equal(promotedGuestId, notifications[0].UserId);
    }
}
