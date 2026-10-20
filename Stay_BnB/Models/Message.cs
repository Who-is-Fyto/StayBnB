using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Stay_BnB.Models;

public class Message
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string SenderId { get; set; } = string.Empty;

    [Required]
    public string ReceiverId { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public bool IsRead { get; set; } = false;

    public int? PropertyId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(SenderId))]
    public virtual ApplicationUser? Sender { get; set; }

    [ForeignKey(nameof(ReceiverId))]
    public virtual ApplicationUser? Receiver { get; set; }

    [ForeignKey(nameof(PropertyId))]
    public virtual HostProperty? Property { get; set; }
}
