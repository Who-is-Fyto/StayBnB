using Microsoft.EntityFrameworkCore;
using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.Models.Enums;
using Xunit;

namespace Stay_BnB.Tests;

public class HostManagementTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void HostDashboard_CalculatesEarningsAndListingCountsAccurately()
    {
        using var context = GetInMemoryDbContext();
        var hostId = "host-dash-1";

        // Arrange: 2 active properties, 1 inactive property
        var prop1 = new HostProperty { Id = 1, HostId = hostId, Title = "Villa Alpha", IsActive = true, PricePerNight = 200m };
        var prop2 = new HostProperty { Id = 2, HostId = hostId, Title = "Loft Beta", IsActive = true, PricePerNight = 150m };
        var prop3 = new HostProperty { Id = 3, HostId = hostId, Title = "Cottage Gamma", IsActive = false, PricePerNight = 100m };
        context.HostProperties.AddRange(prop1, prop2, prop3);

        // Arrange bookings: Approved ($400), Completed ($300), Pending ($250), Cancelled ($150)
        var b1 = new Booking { Id = 1, PropertyId = 1, GuestId = "guest-1", TotalPrice = 400m, Status = BookingStatus.Approved };
        var b2 = new Booking { Id = 2, PropertyId = 2, GuestId = "guest-2", TotalPrice = 300m, Status = BookingStatus.Completed };
        var b3 = new Booking { Id = 3, PropertyId = 1, GuestId = "guest-3", TotalPrice = 250m, Status = BookingStatus.Pending };
        var b4 = new Booking { Id = 4, PropertyId = 2, GuestId = "guest-4", TotalPrice = 150m, Status = BookingStatus.Cancelled };
        context.Bookings.AddRange(b1, b2, b3, b4);
        context.SaveChanges();

        // Act
        var hostProperties = context.HostProperties.Where(p => p.HostId == hostId).ToList();
        int activeCount = hostProperties.Count(p => p.IsActive);
        int inactiveCount = hostProperties.Count(p => !p.IsActive);

        var propertyIds = hostProperties.Select(p => p.Id).ToList();
        var hostBookings = context.Bookings.Where(b => propertyIds.Contains(b.PropertyId)).ToList();

        decimal earnings = hostBookings
            .Where(b => b.Status == BookingStatus.Approved || b.Status == BookingStatus.Completed)
            .Sum(b => b.TotalPrice);

        int pendingCount = hostBookings.Count(b => b.Status == BookingStatus.Pending);

        // Assert
        Assert.Equal(3, hostProperties.Count);
        Assert.Equal(2, activeCount);
        Assert.Equal(1, inactiveCount);
        Assert.Equal(700m, earnings); // 400 + 300
        Assert.Equal(1, pendingCount);
    }

    [Fact]
    public void ToggleActive_SwitchesPropertyVisibility()
    {
        using var context = GetInMemoryDbContext();
        var property = new HostProperty
        {
            Id = 10,
            HostId = "host-toggle",
            Title = "Ocean Breeze",
            IsActive = true
        };
        context.HostProperties.Add(property);
        context.SaveChanges();

        // Act 1: Toggle off
        property.IsActive = !property.IsActive;
        context.SaveChanges();

        // Assert 1
        Assert.False(property.IsActive);

        // Act 2: Toggle on
        property.IsActive = !property.IsActive;
        context.SaveChanges();

        // Assert 2
        Assert.True(property.IsActive);
    }

    [Fact]
    public void HostApproveReservation_UpdatesStatusToApprovedAndCapturesPayment()
    {
        using var context = GetInMemoryDbContext();

        var property = new HostProperty
        {
            Id = 20,
            HostId = "host-approve",
            Title = "City Penthouse",
            IsActive = true
        };
        context.HostProperties.Add(property);

        var booking = new Booking
        {
            Id = 200,
            PropertyId = property.Id,
            GuestId = "guest-approve",
            CheckInDate = new DateTime(2026, 12, 1),
            CheckOutDate = new DateTime(2026, 12, 5),
            TotalPrice = 500m,
            Status = BookingStatus.Pending
        };

        var payment = new Payment
        {
            Id = 200,
            BookingId = booking.Id,
            Amount = 500m,
            Status = PaymentStatus.Pending
        };
        booking.Payment = payment;

        context.Bookings.Add(booking);
        context.SaveChanges();

        // Act: Host approves booking
        booking.Status = BookingStatus.Approved;
        booking.Payment.Status = PaymentStatus.Completed;

        var checkIn = new GuestCheckIn
        {
            BookingId = booking.Id,
            Status = CheckInStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        context.GuestCheckIns.Add(checkIn);
        context.SaveChanges();

        // Assert
        var savedBooking = context.Bookings.Include(b => b.Payment).FirstOrDefault(b => b.Id == 200);
        Assert.NotNull(savedBooking);
        Assert.Equal(BookingStatus.Approved, savedBooking.Status);
        Assert.NotNull(savedBooking.Payment);
        Assert.Equal(PaymentStatus.Completed, savedBooking.Payment.Status);

        var savedCheckIn = context.GuestCheckIns.FirstOrDefault(c => c.BookingId == 200);
        Assert.NotNull(savedCheckIn);
        Assert.Equal(CheckInStatus.Pending, savedCheckIn.Status);
    }

    [Fact]
    public void HostApproveReservation_DetectsConflictingReservation()
    {
        using var context = GetInMemoryDbContext();
        int propertyId = 30;

        // Existing approved booking: Dec 10 to Dec 15
        var confirmedBooking = new Booking
        {
            Id = 301,
            PropertyId = propertyId,
            GuestId = "guest-prior",
            CheckInDate = new DateTime(2026, 12, 10),
            CheckOutDate = new DateTime(2026, 12, 15),
            Status = BookingStatus.Approved,
            TotalPrice = 600m
        };

        // Incoming pending booking: Dec 12 to Dec 17 (overlaps)
        var pendingBooking = new Booking
        {
            Id = 302,
            PropertyId = propertyId,
            GuestId = "guest-overlap",
            CheckInDate = new DateTime(2026, 12, 12),
            CheckOutDate = new DateTime(2026, 12, 17),
            Status = BookingStatus.Pending,
            TotalPrice = 650m
        };

        context.Bookings.AddRange(confirmedBooking, pendingBooking);
        context.SaveChanges();

        // Act: Evaluate double booking check
        bool hasConflict = context.Bookings.Any(b =>
            b.PropertyId == pendingBooking.PropertyId &&
            b.Id != pendingBooking.Id &&
            b.Status == BookingStatus.Approved &&
            b.CheckInDate < pendingBooking.CheckOutDate &&
            b.CheckOutDate > pendingBooking.CheckInDate);

        // Assert
        Assert.True(hasConflict, "Host approval should be blocked when an overlapping confirmed booking exists.");
    }

    [Fact]
    public void HostRejectReservation_UpdatesStatusToRejectedAndRefundsPayment()
    {
        using var context = GetInMemoryDbContext();

        var booking = new Booking
        {
            Id = 400,
            PropertyId = 40,
            GuestId = "guest-reject",
            CheckInDate = new DateTime(2026, 12, 20),
            CheckOutDate = new DateTime(2026, 12, 23),
            TotalPrice = 350m,
            Status = BookingStatus.Pending
        };

        var payment = new Payment
        {
            Id = 400,
            BookingId = booking.Id,
            Amount = 350m,
            Status = PaymentStatus.Pending
        };
        booking.Payment = payment;

        context.Bookings.Add(booking);
        context.SaveChanges();

        // Act: Host declines
        booking.Status = BookingStatus.Rejected;
        booking.Payment.Status = PaymentStatus.Refunded;
        context.SaveChanges();

        // Assert
        var savedBooking = context.Bookings.Include(b => b.Payment).FirstOrDefault(b => b.Id == 400);
        Assert.NotNull(savedBooking);
        Assert.Equal(BookingStatus.Rejected, savedBooking.Status);
        Assert.NotNull(savedBooking.Payment);
        Assert.Equal(PaymentStatus.Refunded, savedBooking.Payment.Status);
    }

    [Fact]
    public void DeleteImage_PreventsDeletingLastImage()
    {
        using var context = GetInMemoryDbContext();

        var property = new HostProperty
        {
            Id = 50,
            HostId = "host-img",
            Title = "Single Photo Cabin",
            IsActive = true
        };
        var singleImage = new PropertyImage
        {
            Id = 501,
            PropertyId = property.Id,
            ImageUrl = "/uploads/properties/cabin.jpg",
            IsCover = true
        };
        property.Images.Add(singleImage);

        context.HostProperties.Add(property);
        context.SaveChanges();

        // Act: Check whether deletion is permitted
        bool canDelete = property.Images.Count > 1;

        // Assert
        Assert.False(canDelete, "Cannot delete when only 1 image remains on the listing.");
    }
}
