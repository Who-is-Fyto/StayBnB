using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Stay_BnB.Models;

public class HostProperty
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerNight { get; set; }

    [Required]
    public string HostId { get; set; } = string.Empty;

    [Required]
    public string Address { get; set; } = string.Empty;

    [StringLength(100)]
    public string City { get; set; } = "Cape Town";

    [StringLength(50)]
    public string PropertyType { get; set; } = "Apartment";

    [Column(TypeName = "decimal(18,2)")]
    public decimal CleaningFee { get; set; } = 25.00m;

    [Column(TypeName = "decimal(18,2)")]
    public decimal ServiceFee { get; set; } = 15.00m;

    public int MaxGuests { get; set; } = 2;

    public bool IsActive { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(HostId))]
    public virtual ApplicationUser? Host { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();
    public virtual ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();
    public virtual CheckInProcess? CheckInProcess { get; set; }
    public virtual ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public virtual ICollection<HostApplication> HostApplications { get; set; } = new List<HostApplication>();
}
