using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.ViewModels.Property;

namespace Stay_BnB.Controllers;

public class PropertiesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PropertiesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Properties
    // Catalog view with multi-criteria filtering
    public async Task<IActionResult> Index(
        string? city,
        string? propertyType,
        decimal? minPrice,
        decimal? maxPrice,
        int? guests,
        List<int>? amenityIds)
    {
        var query = _context.HostProperties
            .Include(p => p.Images)
            .Include(p => p.Amenities)
            .Include(p => p.Reviews)
            .Where(p => p.IsActive);

        // Filter by destination / city
        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(p => p.City.ToLower().Contains(city.ToLower()) || p.Address.ToLower().Contains(city.ToLower()));
        }

        // Filter by property type
        if (!string.IsNullOrWhiteSpace(propertyType))
        {
            query = query.Where(p => p.PropertyType.ToLower() == propertyType.ToLower());
        }

        // Filter by price range
        if (minPrice.HasValue && minPrice > 0)
        {
            query = query.Where(p => p.PricePerNight >= minPrice.Value);
        }
        if (maxPrice.HasValue && maxPrice > 0)
        {
            query = query.Where(p => p.PricePerNight <= maxPrice.Value);
        }

        // Filter by guest capacity
        if (guests.HasValue && guests > 0)
        {
            query = query.Where(p => p.MaxGuests >= guests.Value);
        }

        // Filter by selected amenities
        if (amenityIds != null && amenityIds.Any())
        {
            foreach (var aId in amenityIds)
            {
                query = query.Where(p => p.Amenities.Any(a => a.Id == aId));
            }
        }

        var properties = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();

        // Populate available filter options
        var allCities = await _context.HostProperties.Where(p => p.IsActive).Select(p => p.City).Distinct().ToListAsync();
        var allTypes = await _context.HostProperties.Where(p => p.IsActive).Select(p => p.PropertyType).Distinct().ToListAsync();
        var allAmenities = await _context.Amenities.OrderBy(a => a.Name).ToListAsync();

        var model = new PropertyFilterViewModel
        {
            Properties = properties,
            City = city,
            PropertyType = propertyType,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Guests = guests,
            SelectedAmenityIds = amenityIds ?? new List<int>(),
            AvailableCities = allCities,
            AvailablePropertyTypes = allTypes,
            AvailableAmenities = allAmenities
        };

        return View(model);
    }

    // GET: /Properties/Details/5
    // Detailed listing view with image gallery, amenities, and floating reserve card
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var property = await _context.HostProperties
            .Include(p => p.Host)
            .Include(p => p.Images)
            .Include(p => p.Amenities)
            .Include(p => p.CheckInProcess)
            .Include(p => p.Reviews)
                .ThenInclude(r => r.Reviewer)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

        if (property == null) return NotFound();

        // Check if wishlisted by current user
        bool isWishlisted = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                isWishlisted = await _context.WishlistItems.AnyAsync(w => w.UserId == user.Id && w.PropertyId == property.Id);
            }
        }

        var viewModel = new PropertyDetailsViewModel
        {
            Property = property,
            GalleryImages = property.Images.ToList(),
            Amenities = property.Amenities.ToList(),
            Reviews = property.Reviews.OrderByDescending(r => r.CreatedAt).ToList(),
            AverageRating = property.Reviews.Any() ? property.Reviews.Average(r => r.Rating) : 0.0,
            ReviewCount = property.Reviews.Count,
            CheckInProcess = property.CheckInProcess,
            IsInWishlist = isWishlisted
        };

        return View(viewModel);
    }
}
