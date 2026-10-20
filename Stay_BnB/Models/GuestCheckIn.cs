using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Stay_BnB.Models.Enums;

namespace Stay_BnB.Models;

public class GuestCheckIn
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int BookingId { get; set; }

    public int? CheckInProcessId { get; set; }

    public CheckInStatus Status { get; set; } = CheckInStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(BookingId))]
    public virtual Booking? Booking { get; set; }

    [ForeignKey(nameof(CheckInProcessId))]
    public virtual CheckInProcess? CheckInProcess { get; set; }

    public virtual ICollection<GuestDocument> GuestDocuments { get; set; } = new List<GuestDocument>();
}
