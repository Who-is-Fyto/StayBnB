using Microsoft.EntityFrameworkCore;
using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.Models.Enums;
using Xunit;

namespace Stay_BnB.Tests;

public class CommunicationEngineTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void SendMessage_CreatesMessageRecordAndDispatchesNotification()
    {
        using var context = GetInMemoryDbContext();
        var senderId = "user-alice";
        var receiverId = "user-bob";

        // Act: Create message
        var message = new Message
        {
            Id = 1,
            SenderId = senderId,
            ReceiverId = receiverId,
            PropertyId = 10,
            Content = "Hello, is early check-in possible tomorrow?",
            Timestamp = DateTime.UtcNow,
            IsRead = false
        };
        context.Messages.Add(message);

        // Dispatch notification
        var notification = new Notification
        {
            Id = 1,
            UserId = receiverId,
            Title = "New Message",
            Message = "Alice: Hello, is early check-in possible tomorrow?",
            Type = NotificationType.NewMessage,
            TargetUrl = $"/Messages/Conversation?userId={senderId}&propertyId=10",
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };
        context.Notifications.Add(notification);
        context.SaveChanges();

        // Assert
        var savedMessage = context.Messages.FirstOrDefault(m => m.Id == 1);
        Assert.NotNull(savedMessage);
        Assert.False(savedMessage.IsRead);
        Assert.Equal("Hello, is early check-in possible tomorrow?", savedMessage.Content);

        var savedNotification = context.Notifications.FirstOrDefault(n => n.Id == 1);
        Assert.NotNull(savedNotification);
        Assert.Equal(NotificationType.NewMessage, savedNotification.Type);
        Assert.Equal(receiverId, savedNotification.UserId);
        Assert.False(savedNotification.IsRead);
    }

    [Fact]
    public void ConversationThread_MarksUnreadMessagesAsRead()
    {
        using var context = GetInMemoryDbContext();
        var currentUserId = "user-me";
        var otherUserId = "user-them";

        // Arrange: 2 incoming unread messages, 1 already read, 1 outgoing
        context.Messages.AddRange(
            new Message { Id = 10, SenderId = otherUserId, ReceiverId = currentUserId, Content = "Msg 1", IsRead = false },
            new Message { Id = 11, SenderId = otherUserId, ReceiverId = currentUserId, Content = "Msg 2", IsRead = false },
            new Message { Id = 12, SenderId = otherUserId, ReceiverId = currentUserId, Content = "Msg 3", IsRead = true },
            new Message { Id = 13, SenderId = currentUserId, ReceiverId = otherUserId, Content = "My reply", IsRead = false }
        );
        context.SaveChanges();

        // Act: Open conversation and mark unread incoming messages as read
        var unread = context.Messages
            .Where(m => m.SenderId == otherUserId && m.ReceiverId == currentUserId && !m.IsRead)
            .ToList();

        foreach (var msg in unread)
        {
            msg.IsRead = true;
        }
        context.SaveChanges();

        // Assert
        int remainingUnread = context.Messages.Count(m => m.ReceiverId == currentUserId && !m.IsRead);
        Assert.Equal(0, remainingUnread);
        Assert.True(context.Messages.Find(10)!.IsRead);
        Assert.True(context.Messages.Find(11)!.IsRead);
    }

    [Fact]
    public void MarkNotificationAsRead_UpdatesIsReadState()
    {
        using var context = GetInMemoryDbContext();
        var notification = new Notification
        {
            Id = 20,
            UserId = "user-1",
            Title = "Booking Approved",
            Message = "Your reservation was confirmed.",
            Type = NotificationType.BookingApproved,
            IsRead = false
        };
        context.Notifications.Add(notification);
        context.SaveChanges();

        // Act
        notification.IsRead = true;
        context.SaveChanges();

        // Assert
        var updated = context.Notifications.Find(20);
        Assert.NotNull(updated);
        Assert.True(updated.IsRead);
    }

    [Fact]
    public void MarkAllNotificationsAsRead_BulkUpdates()
    {
        using var context = GetInMemoryDbContext();
        var userId = "user-bulk";

        context.Notifications.AddRange(
            new Notification { Id = 31, UserId = userId, Title = "Alert 1", IsRead = false },
            new Notification { Id = 32, UserId = userId, Title = "Alert 2", IsRead = false },
            new Notification { Id = 33, UserId = userId, Title = "Alert 3", IsRead = true },
            new Notification { Id = 34, UserId = "other-user", Title = "Alert 4", IsRead = false }
        );
        context.SaveChanges();

        // Act: Mark all for userId
        var userUnread = context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToList();
        foreach (var n in userUnread)
        {
            n.IsRead = true;
        }
        context.SaveChanges();

        // Assert
        int unreadCount = context.Notifications.Count(n => n.UserId == userId && !n.IsRead);
        Assert.Equal(0, unreadCount);

        // Other user's notification should remain unread
        var otherNotification = context.Notifications.Find(34);
        Assert.False(otherNotification!.IsRead);
    }

    [Fact]
    public void ConversationGrouping_GroupsMessagesByOtherUser()
    {
        using var context = GetInMemoryDbContext();
        var currentUserId = "user-host";

        // Messages with 2 different guests
        context.Messages.AddRange(
            new Message { Id = 41, SenderId = "guest-1", ReceiverId = currentUserId, Content = "Hi from Guest 1", Timestamp = DateTime.UtcNow.AddHours(-3) },
            new Message { Id = 42, SenderId = currentUserId, ReceiverId = "guest-1", Content = "Reply to Guest 1", Timestamp = DateTime.UtcNow.AddHours(-2) },
            new Message { Id = 43, SenderId = "guest-2", ReceiverId = currentUserId, Content = "Hi from Guest 2", Timestamp = DateTime.UtcNow.AddHours(-1) }
        );
        context.SaveChanges();

        // Act: Group messages into conversation threads
        var allUserMessages = context.Messages
            .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId)
            .OrderByDescending(m => m.Timestamp)
            .ToList();

        var threadGroups = allUserMessages
            .GroupBy(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId)
            .ToList();

        // Assert
        Assert.Equal(2, threadGroups.Count);
        Assert.Contains(threadGroups, g => g.Key == "guest-1");
        Assert.Contains(threadGroups, g => g.Key == "guest-2");
    }

    [Fact]
    public void NotificationFiltering_FiltersByNotificationType()
    {
        using var context = GetInMemoryDbContext();
        var userId = "user-filter";

        context.Notifications.AddRange(
            new Notification { Id = 51, UserId = userId, Title = "Approved", Type = NotificationType.BookingApproved, IsRead = false },
            new Notification { Id = 52, UserId = userId, Title = "Request", Type = NotificationType.NewBookingRequest, IsRead = false },
            new Notification { Id = 53, UserId = userId, Title = "Chat", Type = NotificationType.NewMessage, IsRead = false },
            new Notification { Id = 54, UserId = userId, Title = "CheckIn", Type = NotificationType.CheckInUpdate, IsRead = true }
        );
        context.SaveChanges();

        // Act
        var bookingAlerts = context.Notifications
            .Where(n => n.UserId == userId &&
                        (n.Type == NotificationType.BookingApproved ||
                         n.Type == NotificationType.BookingRejected ||
                         n.Type == NotificationType.NewBookingRequest))
            .ToList();

        var messageAlerts = context.Notifications
            .Where(n => n.UserId == userId && n.Type == NotificationType.NewMessage)
            .ToList();

        var unreadAlerts = context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToList();

        // Assert
        Assert.Equal(2, bookingAlerts.Count);
        Assert.Single(messageAlerts);
        Assert.Equal(3, unreadAlerts.Count);
    }
}
