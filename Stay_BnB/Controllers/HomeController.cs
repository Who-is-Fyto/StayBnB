using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.Services;

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

        // Load wishlist items for authenticated guest
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                ViewBag.WishlistPropertyIds = await _context.WishlistItems
                    .Where(w => w.UserId == userId)
                    .Select(w => w.PropertyId)
                    .ToListAsync();
            }
        }

        return View(properties);
    }

    // POST: /Home/SetCurrency?currency=ZAR
    // Save currency choice in persistent cookie
    [HttpPost]
    public IActionResult SetCurrency(string currency, string? returnUrl)
    {
        var validCurrencies = CurrencyService.Currencies.Select(c => c.Code).ToList();
        var code = validCurrencies.FirstOrDefault(c => c.Equals(currency?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? CurrencyService.DefaultCurrencyCode;

        Response.Cookies.Append(CurrencyService.CookieName, code, new CookieOptions
        {
            Expires = DateTimeOffset.UtcNow.AddYears(1),
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            var currencyInfo = CurrencyService.Currencies.First(c => c.Code == code);
            return Json(new { success = true, currency = code, symbol = currencyInfo.Symbol, rate = currencyInfo.Rate, name = currencyInfo.Name });
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
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
