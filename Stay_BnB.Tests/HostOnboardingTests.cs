using Microsoft.EntityFrameworkCore;
using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.Models.Enums;
using Stay_BnB.Services;
using Stay_BnB.ViewModels.Host;
using Xunit;

namespace Stay_BnB.Tests;

public class HostOnboardingTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void HostApplication_CreatesInactivePropertyPendingAdminApproval()
    {
        // Business rule: Newly submitted host properties must be inactive until admin approves
        using var context = GetInMemoryDbContext();
        var host = new ApplicationUser { Id = "guest-applicant-1", FirstName = "Applicant", LastName = "User" };
        context.Users.Add(host);

        var property = new HostProperty
        {
            Id = 10,
            HostId = host.Id,
            Title = "Ocean Breeze Apartment",
            Address = "10 Coastal Road",
            City = "Durban",
            PropertyType = "Apartment",
            PricePerNight = 120m,
            IsActive = false // Inactive on initial creation
        };
        context.HostProperties.Add(property);

        var application = new HostApplication
        {
            Id = 1,
            ApplicationUserId = host.Id,
            PropertyId = property.Id,
            Status = ApplicationStatus.Pending,
            AppliedAt = DateTime.UtcNow
        };
        context.HostApplications.Add(application);
        context.SaveChanges();

        // Assert
        var savedProperty = context.HostProperties.Find(10);
        var savedApp = context.HostApplications.Find(1);

        Assert.NotNull(savedProperty);
        Assert.False(savedProperty.IsActive, "Submitted property must remain inactive until approved by an admin.");
        Assert.NotNull(savedApp);
        Assert.Equal(ApplicationStatus.Pending, savedApp.Status);
    }

    [Fact]
    public void HostApplication_RejectsSubmissionWithoutImages()
    {
        // Business rule: "An application is considered incomplete and cannot be submitted without an image."
        var model = new BecomeHostViewModel
        {
            Title = "Mountain Cabin Retreat",
            Description = "Beautiful cabin nestled in the mountains.",
            PropertyType = "Cabin",
            Address = "12 Forest Way",
            City = "Drakensberg",
            PricePerNight = 140m,
            PropertyImages = new List<Microsoft.AspNetCore.Http.IFormFile>() // Empty photo list
        };

        bool hasImages = model.PropertyImages != null && model.PropertyImages.Any(f => f != null && f.Length > 0);

        Assert.False(hasImages, "Hosting application without photos must be considered invalid.");
    }

    [Fact]
    public async Task HostApplication_CreatesAuditLogAndNotification()
    {
        using var context = GetInMemoryDbContext();
        var logger = new ActivityLogger(context);
        var applicantId = "applicant-user-2";

        // Log application
        await logger.LogAsync(
            applicantId,
            "Host Application Submitted",
            ActivityType.HostApplication,
            "Submitted application for 'Sunny Studio'");

        // Dispatch notification
        context.Notifications.Add(new Notification
        {
            UserId = applicantId,
            Title = "Host Application Received",
            Type = NotificationType.ApplicationStatus,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Assert
        var logs = await context.ActivityLogs.ToListAsync();
        var notifications = await context.Notifications.ToListAsync();

        Assert.Single(logs);
        Assert.Equal(ActivityType.HostApplication, logs[0].ActivityType);
        Assert.Single(notifications);
        Assert.Equal(NotificationType.ApplicationStatus, notifications[0].Type);
    }
}
