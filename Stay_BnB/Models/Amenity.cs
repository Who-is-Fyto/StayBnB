using System.ComponentModel.DataAnnotations;

namespace Stay_BnB.Models;

public class Amenity
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string IconClass { get; set; } = "bi-check-circle";

    // Navigation property (Many-to-Many)
    public virtual ICollection<HostProperty> Properties { get; set; } = new List<HostProperty>();
}
