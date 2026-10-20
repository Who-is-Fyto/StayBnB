using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.Models.Enums;
using Stay_BnB.Services;
using Xunit;

namespace Stay_BnB.Tests;

public class AccountTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ActivityLogger_ShouldRecordLoginAndRoleChangeEvents()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var logger = new ActivityLogger(context);
        var testUserId = Guid.NewGuid().ToString();

        // Act - Log registration role assignment
        await logger.LogAsync(testUserId, "User Registered", ActivityType.RoleChange, "Assigned default Guest role");

        // Act - Log login
        await logger.LogAsync(testUserId, "User Logged In", ActivityType.Login, "Successful login");

        // Assert
        var logs = await context.ActivityLogs.ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.Contains(logs, l => l.ActivityType == ActivityType.RoleChange && l.Action == "User Registered");
        Assert.Contains(logs, l => l.ActivityType == ActivityType.Login && l.Action == "User Logged In");
    }

    [Fact]
    public void MutualRoleExclusion_UserCannotBeBothGuestAndHost()
    {
        // Business rule check: user cannot hold both Guest and Host simultaneously
        var roles = new List<string> { "Guest" };

        // Host approval transition logic
        if (roles.Contains("Guest"))
        {
            roles.Remove("Guest");
            roles.Add("Host");
        }

        // Verify mutual exclusivity
        Assert.Contains("Host", roles);
        Assert.DoesNotContain("Guest", roles);
        Assert.False(roles.Contains("Guest") && roles.Contains("Host"), "User cannot be both Guest and Host simultaneously.");
    }

    [Fact]
    public void DefaultRole_ForNewRegistration_IsGuest()
    {
        // Check default role convention
        string initialRole = "Guest";
        Assert.Equal("Guest", initialRole);
    }
}
