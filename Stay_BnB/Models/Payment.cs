using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Stay_BnB.Models.Enums;

namespace Stay_BnB.Models;

public class Payment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int BookingId { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property (One-to-One with Booking)
    [ForeignKey(nameof(BookingId))]
    public virtual Booking? Booking { get; set; }
}
