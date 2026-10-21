using Stay_BnB.Models;

namespace Stay_BnB.ViewModels.Property;

public class PropertyFilterViewModel
{
    public IEnumerable<HostProperty> Properties { get; set; } = new List<HostProperty>();

    // Filter parameters
    public string? City { get; set; }
    public string? PropertyType { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? Guests { get; set; }
    public List<int> SelectedAmenityIds { get; set; } = new List<int>();

    // Metadata for dropdowns and filters
    public List<string> AvailableCities { get; set; } = new List<string>();
    public List<string> AvailablePropertyTypes { get; set; } = new List<string>();
    public List<Amenity> AvailableAmenities { get; set; } = new List<Amenity>();

    public int TotalCount => Properties.Count();
}
