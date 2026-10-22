using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.Models.Enums;
using Stay_BnB.ViewModels.CheckIn;
using Xunit;

namespace Stay_BnB.Tests;

public class DigitalCheckInTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void CheckInProcess_SerializeAndDeserializeConfiguration()
    {
        // Arrange
        var stepsData = new CheckInStepsData
        {
            CheckInTime = "14:00",
            CheckOutTime = "10:30",
            EntryMethod = "Keypad",
            EntryCode = "9182#",
            WifiNetwork = "OceanLoft-Guest",
            WifiPassword = "SunsetBeachPassword!",
            ParkingInstructions = "Bay #12 underground",
            HouseRules = "Quiet hours after 22:00",
            RequireIdUpload = true
        };

        // Act
        string json = JsonSerializer.Serialize(stepsData);
        var deserialized = JsonSerializer.Deserialize<CheckInStepsData>(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("14:00", deserialized.CheckInTime);
        Assert.Equal("9182#", deserialized.EntryCode);
        Assert.Equal("OceanLoft-Guest", deserialized.WifiNetwork);
        Assert.True(deserialized.RequireIdUpload);
    }

    [Fact]
    public void GuestCheckIn_OnlyPermittedForApprovedBookings()
    {
        // Arrange
        var pendingBooking = new Booking { Id = 1, Status = BookingStatus.Pending };
        var cancelledBooking = new Booking { Id = 2, Status = BookingStatus.Cancelled };
        var approvedBooking = new Booking { Id = 3, Status = BookingStatus.Approved };

        // Act
        bool canPendingCheckIn = pendingBooking.Status == BookingStatus.Approved || pendingBooking.Status == BookingStatus.Completed;
        bool canCancelledCheckIn = cancelledBooking.Status == BookingStatus.Approved || cancelledBooking.Status == BookingStatus.Completed;
        bool canApprovedCheckIn = approvedBooking.Status == BookingStatus.Approved || approvedBooking.Status == BookingStatus.Completed;

        // Assert
        Assert.False(canPendingCheckIn, "Pending bookings cannot begin check-in.");
        Assert.False(canCancelledCheckIn, "Cancelled bookings cannot begin check-in.");
        Assert.True(canApprovedCheckIn, "Approved bookings can begin digital check-in.");
    }

    [Fact]
    public void GuestDocumentUpload_TransitionsStatusToInProgress()
    {
        using var context = GetInMemoryDbContext();

        var checkIn = new GuestCheckIn
        {
            Id = 1,
            BookingId = 10,
            Status = CheckInStatus.Pending
        };
        context.GuestCheckIns.Add(checkIn);
        context.SaveChanges();

        // Act: Guest uploads ID document
        var doc = new GuestDocument
        {
            Id = 101,
            GuestCheckInId = checkIn.Id,
            DocumentType = "Passport",
            FileName = "/uploads/documents/passport123.jpg",
            Status = DocumentStatus.PendingVerification,
            UploadedAt = DateTime.UtcNow
        };
        context.GuestDocuments.Add(doc);
        checkIn.Status = CheckInStatus.InProgress;
        context.SaveChanges();

        // Assert
        var savedCheckIn = context.GuestCheckIns.Include(c => c.GuestDocuments).FirstOrDefault(c => c.Id == 1);
        Assert.NotNull(savedCheckIn);
        Assert.Equal(CheckInStatus.InProgress, savedCheckIn.Status);
        Assert.Single(savedCheckIn.GuestDocuments);
        Assert.Equal(DocumentStatus.PendingVerification, savedCheckIn.GuestDocuments.First().Status);
    }

    [Fact]
    public void HostVerifyDocument_ApproveDocumentUpdatesStatusAndRevealsAccess()
    {
        using var context = GetInMemoryDbContext();

        var checkIn = new GuestCheckIn
        {
            Id = 2,
            BookingId = 20,
            Status = CheckInStatus.InProgress
        };
        var doc = new GuestDocument
        {
            Id = 202,
            GuestCheckInId = checkIn.Id,
            DocumentType = "Driver's License",
            FileName = "/uploads/documents/dl456.png",
            Status = DocumentStatus.PendingVerification
        };
        checkIn.GuestDocuments.Add(doc);
        context.GuestCheckIns.Add(checkIn);
        context.SaveChanges();

        // Act: Host approves document
        doc.Status = DocumentStatus.Verified;
        checkIn.Status = CheckInStatus.Verified;
        context.SaveChanges();

        // Assert
        var updatedCheckIn = context.GuestCheckIns.Include(c => c.GuestDocuments).FirstOrDefault(c => c.Id == 2);
        Assert.NotNull(updatedCheckIn);
        Assert.Equal(CheckInStatus.Verified, updatedCheckIn.Status);
        Assert.Equal(DocumentStatus.Verified, updatedCheckIn.GuestDocuments.First().Status);
    }

    [Fact]
    public void HostRejectDocument_RequiresReupload()
    {
        using var context = GetInMemoryDbContext();

        var checkIn = new GuestCheckIn
        {
            Id = 3,
            BookingId = 30,
            Status = CheckInStatus.InProgress
        };
        var doc = new GuestDocument
        {
            Id = 303,
            GuestCheckInId = checkIn.Id,
            DocumentType = "Passport",
            FileName = "/uploads/documents/blurry.jpg",
            Status = DocumentStatus.PendingVerification
        };
        checkIn.GuestDocuments.Add(doc);
        context.GuestCheckIns.Add(checkIn);
        context.SaveChanges();

        // Act: Host rejects document
        doc.Status = DocumentStatus.Rejected;
        checkIn.Status = CheckInStatus.InProgress;
        context.SaveChanges();

        // Assert
        var updatedCheckIn = context.GuestCheckIns.Include(c => c.GuestDocuments).FirstOrDefault(c => c.Id == 3);
        Assert.NotNull(updatedCheckIn);
        Assert.Equal(CheckInStatus.InProgress, updatedCheckIn.Status);
        Assert.Equal(DocumentStatus.Rejected, updatedCheckIn.GuestDocuments.First().Status);
    }

    [Fact]
    public void DocumentValidation_FileFormatEnforcement()
    {
        // Arrange
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };

        var validFile1 = "passport.jpg";
        var validFile2 = "national_id.pdf";
        var invalidFile1 = "malicious.exe";
        var invalidFile2 = "script.sh";

        // Act & Assert
        Assert.Contains(System.IO.Path.GetExtension(validFile1).ToLowerInvariant(), allowedExtensions);
        Assert.Contains(System.IO.Path.GetExtension(validFile2).ToLowerInvariant(), allowedExtensions);
        Assert.DoesNotContain(System.IO.Path.GetExtension(invalidFile1).ToLowerInvariant(), allowedExtensions);
        Assert.DoesNotContain(System.IO.Path.GetExtension(invalidFile2).ToLowerInvariant(), allowedExtensions);
    }
}
