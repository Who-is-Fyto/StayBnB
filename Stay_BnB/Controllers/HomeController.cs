using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stay_BnB.Data;
using Stay_BnB.Models;

namespace Stay_BnB.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Display active property catalog with filters
    public async Task<IActionResult> Index(string? city, string? propertyType, decimal? maxPrice)
    {
        var query = _context.HostProperties
            .Include(p => p.Images)
            .Include(p => p.Reviews)
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(city))
        {
            query = query.Where(p => p.City.ToLower().Contains(city.ToLower()) || p.Address.ToLower().Contains(city.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(propertyType))
        {
            query = query.Where(p => p.PropertyType.ToLower() == propertyType.ToLower());
        }

        if (maxPrice.HasValue && maxPrice > 0)
        {
            query = query.Where(p => p.PricePerNight <= maxPrice.Value);
        }

        var properties = await query.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return View(properties);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
