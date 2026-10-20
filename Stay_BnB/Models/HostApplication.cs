using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Stay_BnB.Models.Enums;

namespace Stay_BnB.Models;

public class HostApplication
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string ApplicationUserId { get; set; } = string.Empty;

    [Required]
    public int PropertyId { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;

    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    [StringLength(500)]
    public string? RejectionReason { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ApplicationUserId))]
    public virtual ApplicationUser? ApplicationUser { get; set; }

    [ForeignKey(nameof(PropertyId))]
    public virtual HostProperty? Property { get; set; }
}
