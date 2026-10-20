using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Stay_BnB.Models;

public class CheckInProcess
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PropertyId { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = "Standard Check-in";

    public string StepsJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property (One-to-One with HostProperty)
    [ForeignKey(nameof(PropertyId))]
    public virtual HostProperty? Property { get; set; }

    public virtual ICollection<GuestCheckIn> GuestCheckIns { get; set; } = new List<GuestCheckIn>();
}
