using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Stay_BnB.Models;

public class PropertyImage
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int PropertyId { get; set; }

    [Required]
    public string ImageUrl { get; set; } = string.Empty;

    public bool IsCover { get; set; } = false;

    // Navigation property
    [ForeignKey(nameof(PropertyId))]
    public virtual HostProperty? Property { get; set; }
}
