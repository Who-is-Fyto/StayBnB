using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Stay_BnB.Models;

public class Review
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PropertyId { get; set; }

    [Required]
    public string ReviewerId { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Rating { get; set; }

    [Required]
    [StringLength(1000)]
    public string Comment { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(PropertyId))]
    public virtual HostProperty? Property { get; set; }

    [ForeignKey(nameof(ReviewerId))]
    public virtual ApplicationUser? Reviewer { get; set; }
}
