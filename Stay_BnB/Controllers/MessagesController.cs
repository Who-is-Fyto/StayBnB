using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.Models.Enums;
using Stay_BnB.Services;
using Stay_BnB.ViewModels.Communication;

namespace Stay_BnB.Controllers;

[Authorize]
public class MessagesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IActivityLogger _activityLogger;

    public MessagesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IActivityLogger activityLogger)
    {
        _context = context;
        _userManager = userManager;
        _activityLogger = activityLogger;
    }

    // GET: /Messages
    // Inbox view with active conversation threads
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var messages = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Include(m => m.Property)
                .ThenInclude(p => p!.Images)
            .Where(m => m.SenderId == user.Id || m.ReceiverId == user.Id)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync();

        // Group messages into distinct conversation threads by other user
        var threadGroups = messages
            .GroupBy(m => m.SenderId == user.Id ? m.ReceiverId : m.SenderId)
            .ToList();

        var threads = new List<ConversationThreadViewModel>();

        foreach (var group in threadGroups)
        {
            var otherUserId = group.Key;
            var latestMessage = group.First();
            var otherUser = latestMessage.SenderId == user.Id ? latestMessage.Receiver : latestMessage.Sender;

            if (otherUser == null) continue;

            var prop = latestMessage.Property;
            var coverImage = prop?.Images.FirstOrDefault(i => i.IsCover)?.ImageUrl 
                ?? prop?.Images.FirstOrDefault()?.ImageUrl;

            int unreadCount = group.Count(m => m.ReceiverId == user.Id && !m.IsRead);

            threads.Add(new ConversationThreadViewModel
            {
                OtherUserId = otherUserId,
                OtherUserName = otherUser.FullName,
                OtherUserEmail = otherUser.Email,
                PropertyId = prop?.Id,
                PropertyTitle = prop?.Title,
                PropertyImage = coverImage,
                LastMessage = latestMessage.Content,
                LastMessageTimestamp = latestMessage.Timestamp,
                UnreadCount = unreadCount
            });
        }

        var model = new MessagesInboxViewModel
        {
            Threads = threads.OrderByDescending(t => t.LastMessageTimestamp).ToList(),
            TotalUnread = threads.Sum(t => t.UnreadCount)
        };

        return View(model);
    }

    // GET: /Messages/Conversation?userId=xyz&propertyId=1
    // Conversation thread view between current user and another party
    [HttpGet]
    public async Task<IActionResult> Conversation(string userId, int? propertyId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (string.IsNullOrWhiteSpace(userId) || userId == user.Id)
        {
            return RedirectToAction(nameof(Index));
        }

        var otherUser = await _userManager.FindByIdAsync(userId);
        if (otherUser == null) return NotFound();

        var messages = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Include(m => m.Property)
            .Where(m => (m.SenderId == user.Id && m.ReceiverId == userId) ||
                        (m.SenderId == userId && m.ReceiverId == user.Id))
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        // Mark incoming unread messages as read
        var unread = messages.Where(m => m.ReceiverId == user.Id && !m.IsRead).ToList();
        if (unread.Any())
        {
            foreach (var msg in unread)
            {
                msg.IsRead = true;
            }
            await _context.SaveChangesAsync();
        }

        // Determine context property
        HostProperty? property = null;
        if (propertyId.HasValue)
        {
            property = await _context.HostProperties
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == propertyId.Value);
        }
        else
        {
            var msgWithProp = messages.LastOrDefault(m => m.Property != null);
            if (msgWithProp != null)
            {
                property = await _context.HostProperties
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == msgWithProp.PropertyId);
            }
        }

        var model = new ConversationDetailViewModel
        {
            OtherUserId = otherUser.Id,
            OtherUserName = otherUser.FullName,
            OtherUserEmail = otherUser.Email,
            Property = property,
            Messages = messages,
            CurrentUserId = user.Id
        };

        return View(model);
    }

    // POST: /Messages/Send
    // Deliver new message and notify recipient
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(string receiverId, int? propertyId, string content)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (string.IsNullOrWhiteSpace(receiverId) || string.IsNullOrWhiteSpace(content))
        {
            return RedirectToAction(nameof(Conversation), new { userId = receiverId, propertyId });
        }

        var message = new Message
        {
            SenderId = user.Id,
            ReceiverId = receiverId,
            PropertyId = propertyId,
            Content = content.Trim(),
            Timestamp = DateTime.UtcNow,
            IsRead = false
        };

        _context.Messages.Add(message);

        // Preview snippet for notification
        var snippet = content.Trim();
        if (snippet.Length > 60)
        {
            snippet = snippet.Substring(0, 57) + "...";
        }

        // Dispatch notification to receiver
        _context.Notifications.Add(new Notification
        {
            UserId = receiverId,
            Title = "New Message",
            Message = $"{user.FullName}: {snippet}",
            Type = NotificationType.NewMessage,
            TargetUrl = $"/Messages/Conversation?userId={user.Id}&propertyId={propertyId}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Conversation), new { userId = receiverId, propertyId });
    }

    // GET: /Messages/ContactHost?propertyId=5
    // Shortcut to message the host of a property
    [HttpGet]
    public async Task<IActionResult> ContactHost(int propertyId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var property = await _context.HostProperties.FirstOrDefaultAsync(p => p.Id == propertyId);
        if (property == null) return NotFound();

        if (property.HostId == user.Id)
        {
            TempData["ErrorMessage"] = "You cannot send messages to yourself.";
            return RedirectToAction("Details", "Properties", new { id = propertyId });
        }

        return RedirectToAction(nameof(Conversation), new { userId = property.HostId, propertyId = property.Id });
    }
}
