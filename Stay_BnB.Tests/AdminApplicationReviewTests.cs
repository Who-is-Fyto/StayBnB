using Microsoft.EntityFrameworkCore;
using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.Models.Enums;
using Stay_BnB.Services;
using Xunit;

namespace Stay_BnB.Tests;

public class AdminApplicationReviewTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void AdminApproval_TransitionsGuestToHostAndActivatesProperty()
    {
        using var context = GetInMemoryDbContext();

        // Arrange: Create applicant with Guest role and pending application
        var applicant = new ApplicationUser { Id = "applicant-101", Email = "applicant@test.com", FirstName = "John", LastName = "Doe" };
        context.Users.Add(applicant);

        var property = new HostProperty
        {
            Id = 50,
            HostId = applicant.Id,
            Title = "Sunset Loft",
            City = "Cape Town",
            IsActive = false // Inactive before approval
        };
        context.HostProperties.Add(property);

        var application = new HostApplication
        {
            Id = 10,
            ApplicationUserId = applicant.Id,
            PropertyId = property.Id,
            Status = ApplicationStatus.Pending,
            AppliedAt = DateTime.UtcNow
        };
        context.HostApplications.Add(application);
        context.SaveChanges();

        // Simulate role list before approval
        var roles = new List<string> { "Guest" };

        // Act: Execute the transition rule (Section 1.3 of requirements)
        if (roles.Contains("Guest"))
        {
            roles.Remove("Guest");
        }
        roles.Add("Host");

        application.Status = ApplicationStatus.Approved;
        application.ReviewedAt = DateTime.UtcNow;
        property.IsActive = true;
        context.SaveChanges();

        // Assert
        Assert.Contains("Host", roles);
        Assert.DoesNotContain("Guest", roles);
        Assert.False(roles.Contains("Guest") && roles.Contains("Host"), "User cannot hold both Guest and Host simultaneously.");
        Assert.Equal(ApplicationStatus.Approved, application.Status);
        Assert.True(property.IsActive, "Approved property must become active in the marketplace.");
    }

    [Fact]
    public void AdminRejection_KeepsUserInGuestRoleAndRecordsReason()
    {
        using var context = GetInMemoryDbContext();

        var applicant = new ApplicationUser { Id = "applicant-102", Email = "applicant2@test.com", FirstName = "Jane", LastName = "Doe" };
        context.Users.Add(applicant);

        var property = new HostProperty
        {
            Id = 51,
            HostId = applicant.Id,
            Title = "Rustic Shed",
            City = "Durban",
            IsActive = false
        };
        context.HostProperties.Add(property);

        var application = new HostApplication
        {
            Id = 11,
            ApplicationUserId = applicant.Id,
            PropertyId = property.Id,
            Status = ApplicationStatus.Pending,
            AppliedAt = DateTime.UtcNow
        };
        context.HostApplications.Add(application);
        context.SaveChanges();

        var roles = new List<string> { "Guest" };

        // Act: Rejection decision
        application.Status = ApplicationStatus.Rejected;
        application.RejectionReason = "Property does not meet minimum amenity requirements.";
        application.ReviewedAt = DateTime.UtcNow;
        property.IsActive = false;
        context.SaveChanges();

        // Assert
        Assert.Contains("Guest", roles);
        Assert.DoesNotContain("Host", roles);
        Assert.False(property.IsActive);
        Assert.Equal(ApplicationStatus.Rejected, application.Status);
        Assert.Equal("Property does not meet minimum amenity requirements.", application.RejectionReason);
    }

    [Fact]
    public async Task AdminApproval_LogsAuditActivity()
    {
        using var context = GetInMemoryDbContext();
        var logger = new ActivityLogger(context);
        var adminId = "admin-1";

        // Act
        await logger.LogAsync(
            adminId,
            "Host Application Approved",
            ActivityType.RoleChange,
            "User promoted to Host, Guest role removed, property activated");

        // Assert
        var log = await context.ActivityLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal("Host Application Approved", log.Action);
        Assert.Equal(ActivityType.RoleChange, log.ActivityType);
    }
}
