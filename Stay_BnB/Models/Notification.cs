using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Stay_BnB.Models.Enums;

namespace Stay_BnB.Models;

public class Notification
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    public string? Message { get; set; }

    public NotificationType Type { get; set; } = NotificationType.SystemAlert;

    public bool IsRead { get; set; } = false;

    public string? TargetUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }
}
