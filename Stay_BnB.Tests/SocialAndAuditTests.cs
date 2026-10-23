using Microsoft.EntityFrameworkCore;
using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.Models.Enums;
using Stay_BnB.Services;
using Xunit;

namespace Stay_BnB.Tests;

public class SocialAndAuditTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void WishlistToggle_AddsPropertyWhenNotPresent()
    {
        using var context = GetInMemoryDbContext();
        var guestId = "guest-10";
        var propertyId = 42;

        // Arrange: Property exists
        var property = new HostProperty
        {
            Id = propertyId,
            HostId = "host-1",
            Title = "Ocean Breeze Villa",
            City = "Durban",
            IsActive = true
        };
        context.HostProperties.Add(property);
        context.SaveChanges();

        // Check not present initially
        var existing = context.WishlistItems.FirstOrDefault(w => w.UserId == guestId && w.PropertyId == propertyId);
        Assert.Null(existing);

        // Act: Add to wishlist
        context.WishlistItems.Add(new WishlistItem
        {
            UserId = guestId,
            PropertyId = propertyId,
            CreatedAt = DateTime.UtcNow
        });
        context.SaveChanges();

        // Assert
        var savedItem = context.WishlistItems.FirstOrDefault(w => w.UserId == guestId && w.PropertyId == propertyId);
        Assert.NotNull(savedItem);
        Assert.Equal(propertyId, savedItem.PropertyId);
        Assert.Equal(guestId, savedItem.UserId);
    }

    [Fact]
    public void WishlistToggle_RemovesPropertyWhenAlreadyPresent()
    {
        using var context = GetInMemoryDbContext();
        var guestId = "guest-10";
        var propertyId = 42;

        // Arrange: Item already in wishlist
        var existingItem = new WishlistItem
        {
            Id = 1,
            UserId = guestId,
            PropertyId = propertyId,
            CreatedAt = DateTime.UtcNow
        };
        context.WishlistItems.Add(existingItem);
        context.SaveChanges();

        // Act: Remove item
        context.WishlistItems.Remove(existingItem);
        context.SaveChanges();

        // Assert
        var remaining = context.WishlistItems.FirstOrDefault(w => w.UserId == guestId && w.PropertyId == propertyId);
        Assert.Null(remaining);
    }

    [Fact]
    public void ReviewSubmission_AllowedOnlyForGuestsWithConfirmedStays()
    {
        using var context = GetInMemoryDbContext();
        var guestWithStayId = "guest-stayed";
        var guestWithoutStayId = "guest-no-stay";
        var propertyId = 15;

        // Arrange: Property and completed booking for guestWithStayId
        var property = new HostProperty
        {
            Id = propertyId,
            HostId = "host-2",
            Title = "Mountain Retreat",
            City = "Drakensberg",
            IsActive = true
        };
        context.HostProperties.Add(property);

        var approvedBooking = new Booking
        {
            Id = 1,
            PropertyId = propertyId,
            GuestId = guestWithStayId,
            CheckInDate = DateTime.UtcNow.AddDays(-10),
            CheckOutDate = DateTime.UtcNow.AddDays(-5),
            Status = BookingStatus.Completed,
            TotalPrice = 500m
        };
        context.Bookings.Add(approvedBooking);

        var pendingBooking = new Booking
        {
            Id = 2,
            PropertyId = propertyId,
            GuestId = guestWithoutStayId,
            CheckInDate = DateTime.UtcNow.AddDays(5),
            CheckOutDate = DateTime.UtcNow.AddDays(8),
            Status = BookingStatus.Pending,
            TotalPrice = 350m
        };
        context.Bookings.Add(pendingBooking);
        context.SaveChanges();

        // Act: Check eligibility logic
        bool eligibleGuestCanReview = context.Bookings.Any(b =>
            b.PropertyId == propertyId &&
            b.GuestId == guestWithStayId &&
            (b.Status == BookingStatus.Approved || b.Status == BookingStatus.Completed));

        bool ineligibleGuestCanReview = context.Bookings.Any(b =>
            b.PropertyId == propertyId &&
            b.GuestId == guestWithoutStayId &&
            (b.Status == BookingStatus.Approved || b.Status == BookingStatus.Completed));

        // Assert
        Assert.True(eligibleGuestCanReview);
        Assert.False(ineligibleGuestCanReview);
    }

    [Fact]
    public void ReviewSubmission_PreventsDuplicateReviews()
    {
        using var context = GetInMemoryDbContext();
        var guestId = "guest-reviewed";
        var propertyId = 20;

        // Arrange: Existing review from this guest
        var initialReview = new Review
        {
            Id = 1,
            PropertyId = propertyId,
            ReviewerId = guestId,
            Rating = 5,
            Comment = "Exceptional stay, very clean and peaceful!",
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        context.Reviews.Add(initialReview);
        context.SaveChanges();

        // Act: Check duplicate rule
        bool alreadyReviewed = context.Reviews.Any(r =>
            r.PropertyId == propertyId && r.ReviewerId == guestId);

        // Assert
        Assert.True(alreadyReviewed);
    }

    [Fact]
    public void ReviewSubmission_ValidatesRatingRangeAndMinCommentLength()
    {
        // Rating validity helper
        bool IsRatingValid(int rating) => rating >= 1 && rating <= 5;

        // Comment validity helper (min 5 chars)
        bool IsCommentValid(string? comment) => !string.IsNullOrWhiteSpace(comment) && comment.Trim().Length >= 5;

        // Assert rating checks
        Assert.True(IsRatingValid(1));
        Assert.True(IsRatingValid(3));
        Assert.True(IsRatingValid(5));
        Assert.False(IsRatingValid(0));
        Assert.False(IsRatingValid(6));

        // Assert comment checks
        Assert.True(IsCommentValid("Great stay!"));
        Assert.False(IsCommentValid(""));
        Assert.False(IsCommentValid("  "));
        Assert.False(IsCommentValid("Nice")); // 4 chars
    }

    [Fact]
    public void AuditLogs_FiltersByActivityTypeAndSearchTerm()
    {
        using var context = GetInMemoryDbContext();

        var adminUser = new ApplicationUser { Id = "admin-1", FirstName = "Sarah", LastName = "Connor", Email = "sarah@staybnb.com" };
        var guestUser = new ApplicationUser { Id = "guest-2", FirstName = "Kyle", LastName = "Reese", Email = "kyle@guest.com" };
        context.Users.AddRange(adminUser, guestUser);

        // Arrange: Seed various activity logs
        context.ActivityLogs.AddRange(
            new ActivityLog
            {
                Id = 1,
                UserId = adminUser.Id,
                Action = "Role Promoted",
                ActivityType = ActivityType.RoleChange,
                Details = "User Kyle Reese promoted to Admin.",
                CreatedAt = DateTime.UtcNow.AddMinutes(-30)
            },
            new ActivityLog
            {
                Id = 2,
                UserId = adminUser.Id,
                Action = "Role Demoted",
                ActivityType = ActivityType.RoleChange,
                Details = "User John Doe demoted to Guest.",
                CreatedAt = DateTime.UtcNow.AddHours(-2)
            },
            new ActivityLog
            {
                Id = 3,
                UserId = guestUser.Id,
                Action = "Review Submitted",
                ActivityType = ActivityType.BookingUpdate,
                Details = "Guest submitted a 5-star review for 'Sunset Loft'.",
                CreatedAt = DateTime.UtcNow.AddHours(-3)
            },
            new ActivityLog
            {
                Id = 4,
                UserId = adminUser.Id,
                Action = "Suspicious Login Alert",
                ActivityType = ActivityType.SecurityEvent,
                Details = "Multiple failed login attempts detected.",
                CreatedAt = DateTime.UtcNow.AddHours(-5)
            }
        );
        context.SaveChanges();

        // Act 1: Filter by ActivityType.RoleChange
        var roleChangeLogs = context.ActivityLogs
            .Where(a => a.ActivityType == ActivityType.RoleChange)
            .ToList();

        // Act 2: Search by keyword "promoted"
        var searchLogs = context.ActivityLogs
            .Where(a => a.Action.ToLower().Contains("promoted") || (a.Details != null && a.Details.ToLower().Contains("promoted")))
            .ToList();

        // Assert
        Assert.Equal(2, roleChangeLogs.Count);
        Assert.Single(searchLogs);
        Assert.Equal("Role Promoted", searchLogs[0].Action);
    }

    [Fact]
    public void CurrencyService_DefaultsToSouthAfricanRands()
    {
        // Arrange: Currency service with empty cookie context
        var httpContextAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor();
        var service = new CurrencyService(httpContextAccessor);

        // Assert
        Assert.Equal("ZAR", service.CurrentCode);
        Assert.Equal("R", service.CurrentSymbol);
        Assert.Equal(1.0m, service.CurrentRate);
        Assert.Equal("South African Rand", service.CurrentCurrency.Name);
    }

    [Fact]
    public void CurrencyService_ConvertsAndFormatsCurrenciesCorrectly()
    {
        var httpContextAccessor = new Microsoft.AspNetCore.Http.HttpContextAccessor();
        var service = new CurrencyService(httpContextAccessor);

        // Test base ZAR formatting
        var formattedZar = service.Format(1500m, "ZAR");
        Assert.Equal("R1,500", formattedZar);

        // Test USD conversion and formatting
        var usdConverted = service.Convert(1000m, "USD");
        Assert.Equal(55m, usdConverted); // 1000 * 0.055

        var formattedUsd = service.Format(1000m, "USD");
        Assert.Equal("$55.00", formattedUsd);

        // Test EUR formatting
        var formattedEur = service.Format(1000m, "EUR");
        Assert.Equal("€51.00", formattedEur);

        // Test GBP formatting
        var formattedGbp = service.Format(1000m, "GBP");
        Assert.Equal("£43.00", formattedGbp);
    }
}
