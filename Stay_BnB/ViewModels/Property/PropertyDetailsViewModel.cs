using Stay_BnB.Models;

namespace Stay_BnB.ViewModels.Property;

public class PropertyDetailsViewModel
{
    public HostProperty Property { get; set; } = null!;
    public List<PropertyImage> GalleryImages { get; set; } = new List<PropertyImage>();
    public List<Amenity> Amenities { get; set; } = new List<Amenity>();
    public List<Review> Reviews { get; set; } = new List<Review>();
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
    public CheckInProcess? CheckInProcess { get; set; }
    public bool IsInWishlist { get; set; }
}
