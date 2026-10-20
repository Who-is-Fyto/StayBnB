using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Stay_BnB.Models.Enums;

namespace Stay_BnB.Models;

public class ActivityLog
{
    [Key]
    public int Id { get; set; }

    public string? UserId { get; set; }

    [Required]
    [StringLength(100)]
    public string Action { get; set; } = string.Empty;

    public ActivityType ActivityType { get; set; } = ActivityType.Login;

    public string? Details { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    [ForeignKey(nameof(UserId))]
    public virtual ApplicationUser? User { get; set; }
}
