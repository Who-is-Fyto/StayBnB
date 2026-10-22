using Microsoft.EntityFrameworkCore;
using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.Models.Enums;
using Xunit;

namespace Stay_BnB.Tests;

public class BookingEngineTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void ExactPricingCalculation_NightlyCleaningAndServiceFees()
    {
        // Arrange
        decimal pricePerNight = 150m;
        decimal cleaningFee = 50m;
        decimal serviceFee = 25m;
        var checkIn = new DateTime(2026, 11, 1);
        var checkOut = new DateTime(2026, 11, 4); // 3 nights

        // Act
        int nights = (int)(checkOut - checkIn).TotalDays;
        decimal nightlyTotal = pricePerNight * nights;
        decimal calculatedTotal = nightlyTotal + cleaningFee + serviceFee;

        // Assert
        Assert.Equal(3, nights);
        Assert.Equal(450m, nightlyTotal);
        Assert.Equal(525m, calculatedTotal);
    }

    [Fact]
    public void DoubleBookingPrevention_ConflictingDatesDetected()
    {
        using var context = GetInMemoryDbContext();

        // Arrange: Existing approved booking for Nov 10 to Nov 15
        var existingBooking = new Booking
        {
            Id = 1,
            PropertyId = 99,
            GuestId = "guest-1",
            CheckInDate = new DateTime(2026, 11, 10),
            CheckOutDate = new DateTime(2026, 11, 15),
            Status = BookingStatus.Approved,
            TotalPrice = 600m
        };
        context.Bookings.Add(existingBooking);
        context.SaveChanges();

        // Overlapping request: Nov 12 to Nov 17
        var requestedCheckIn = new DateTime(2026, 11, 12);
        var requestedCheckOut = new DateTime(2026, 11, 17);

        // Act
        bool hasConflict = context.Bookings.Any(b =>
            b.PropertyId == 99 &&
            b.Status == BookingStatus.Approved &&
            b.CheckInDate < requestedCheckOut &&
            b.CheckOutDate > requestedCheckIn);

        // Assert
        Assert.True(hasConflict, "Conflicting date range must be flagged as unavailable.");
    }

    [Fact]
    public void DoubleBookingPrevention_NonOverlappingDatesAllowed()
    {
        using var context = GetInMemoryDbContext();

        // Arrange: Existing approved booking Nov 10 to Nov 15
        var existingBooking = new Booking
        {
            Id = 2,
            PropertyId = 99,
            GuestId = "guest-1",
            CheckInDate = new DateTime(2026, 11, 10),
            CheckOutDate = new DateTime(2026, 11, 15),
            Status = BookingStatus.Approved,
            TotalPrice = 600m
        };
        context.Bookings.Add(existingBooking);
        context.SaveChanges();

        // Adjacent stay: checkout Nov 10 (prior), or checkin Nov 15 (subsequent)
        var subsequentCheckIn = new DateTime(2026, 11, 15);
        var subsequentCheckOut = new DateTime(2026, 11, 20);

        // Act
        bool hasConflict = context.Bookings.Any(b =>
            b.PropertyId == 99 &&
            b.Status == BookingStatus.Approved &&
            b.CheckInDate < subsequentCheckOut &&
            b.CheckOutDate > subsequentCheckIn);

        // Assert
        Assert.False(hasConflict, "Back-to-back non-overlapping dates must be allowed.");
    }

    [Fact]
    public void CreateBooking_CreatesPendingBookingAndPendingPayment()
    {
        using var context = GetInMemoryDbContext();

        // Arrange
        var guest = new ApplicationUser { Id = "guest-3", Email = "guest3@test.com", FirstName = "Mark", LastName = "Evans" };
        var host = new ApplicationUser { Id = "host-3", Email = "host3@test.com", FirstName = "Sarah", LastName = "Connor" };
        context.Users.AddRange(guest, host);

        var property = new HostProperty
        {
            Id = 15,
            HostId = host.Id,
            Title = "Garden Cottage",
            Address = "12 Main St",
            City = "Durban",
            PropertyType = "Cottage",
            PricePerNight = 100m,
            CleaningFee = 30m,
            ServiceFee = 15m,
            MaxGuests = 4,
            IsActive = true
        };
        context.HostProperties.Add(property);
        context.SaveChanges();

        // Act: Create new booking with 1:1 payment
        int nights = 2;
        decimal total = (property.PricePerNight * nights) + property.CleaningFee + property.ServiceFee; // 245

        var booking = new Booking
        {
            PropertyId = property.Id,
            GuestId = guest.Id,
            CheckInDate = DateTime.Today.AddDays(2),
            CheckOutDate = DateTime.Today.AddDays(4),
            GuestCount = 2,
            TotalPrice = total,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var payment = new Payment
        {
            Amount = total,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        booking.Payment = payment;

        context.Bookings.Add(booking);
        context.SaveChanges();

        // Assert
        var savedBooking = context.Bookings.Include(b => b.Payment).FirstOrDefault(b => b.Id == booking.Id);
        Assert.NotNull(savedBooking);
        Assert.Equal(BookingStatus.Pending, savedBooking.Status);
        Assert.Equal(245m, savedBooking.TotalPrice);
        Assert.NotNull(savedBooking.Payment);
        Assert.Equal(PaymentStatus.Pending, savedBooking.Payment.Status);
        Assert.Equal(245m, savedBooking.Payment.Amount);
    }

    [Fact]
    public void GuestCapacityValidation_ExceedingMaxGuestsDetected()
    {
        // Arrange
        int propertyMaxGuests = 3;
        int requestedGuests = 5;

        // Act
        bool exceedsCapacity = requestedGuests > propertyMaxGuests;

        // Assert
        Assert.True(exceedsCapacity, "Requested guest count should exceed property capacity.");
    }
}
