using System.ComponentModel.DataAnnotations;
using Stay_BnB.Models;

namespace Stay_BnB.ViewModels.Communication;

public class ConversationThreadViewModel
{
    public string OtherUserId { get; set; } = string.Empty;
    public string OtherUserName { get; set; } = string.Empty;
    public string? OtherUserEmail { get; set; }
    public int? PropertyId { get; set; }
    public string? PropertyTitle { get; set; }
    public string? PropertyImage { get; set; }
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastMessageTimestamp { get; set; }
    public int UnreadCount { get; set; }
}

public class MessagesInboxViewModel
{
    public List<ConversationThreadViewModel> Threads { get; set; } = new List<ConversationThreadViewModel>();
    public int TotalUnread { get; set; }
}

public class ConversationDetailViewModel
{
    public string OtherUserId { get; set; } = string.Empty;
    public string OtherUserName { get; set; } = string.Empty;
    public string? OtherUserEmail { get; set; }
    public HostProperty? Property { get; set; }
    public List<Message> Messages { get; set; } = new List<Message>();
    public string CurrentUserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Message cannot be empty")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Message must be between 1 and 2000 characters")]
    public string ReplyContent { get; set; } = string.Empty;
}

public class NotificationsViewModel
{
    public List<Notification> Notifications { get; set; } = new List<Notification>();
    public string CurrentFilter { get; set; } = "All";
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
}
