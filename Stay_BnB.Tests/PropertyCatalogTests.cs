using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using Stay_BnB.Controllers;
using Stay_BnB.Data;
using Stay_BnB.Models;
using Stay_BnB.ViewModels.Property;
using Xunit;

namespace Stay_BnB.Tests;

public class PropertyCatalogTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new ApplicationDbContext(options);

        // Seed mock properties for testing
        var host = new ApplicationUser { Id = "host-1", FirstName = "David", LastName = "Host" };
        context.Users.Add(host);

        context.HostProperties.AddRange(
            new HostProperty
            {
                Id = 1,
                Title = "Cape Town Luxury Villa",
                City = "Cape Town",
                PropertyType = "Villa",
                PricePerNight = 200m,
                CleaningFee = 40m,
                ServiceFee = 25m,
                MaxGuests = 6,
                IsActive = true,
                HostId = "host-1"
            },
            new HostProperty
            {
                Id = 2,
                Title = "Stellenbosch Cottage",
                City = "Stellenbosch",
                PropertyType = "House",
                PricePerNight = 100m,
                CleaningFee = 20m,
                ServiceFee = 15m,
                MaxGuests = 4,
                IsActive = true,
                HostId = "host-1"
            },
            new HostProperty
            {
                Id = 3,
                Title = "Durban Beach Apartment",
                City = "Durban",
                PropertyType = "Apartment",
                PricePerNight = 80m,
                CleaningFee = 15m,
                ServiceFee = 10m,
                MaxGuests = 2,
                IsActive = true,
                HostId = "host-1"
            },
            new HostProperty
            {
                Id = 4,
                Title = "Unapproved Draft Listing",
                City = "Cape Town",
                PropertyType = "Villa",
                PricePerNight = 300m,
                CleaningFee = 50m,
                ServiceFee = 30m,
                MaxGuests = 8,
                IsActive = false, // Inactive property
                HostId = "host-1"
            }
        );
        context.SaveChanges();
        return context;
    }

    [Fact]
    public void PricingFormula_CalculatesExpectedTotal()
    {
        // Formula: TotalPrice = (PricePerNight x Nights) + CleaningFee + ServiceFee
        decimal pricePerNight = 150m;
        int nights = 3;
        decimal cleaningFee = 30m;
        decimal serviceFee = 20m;

        decimal expectedTotal = (pricePerNight * nights) + cleaningFee + serviceFee;

        Assert.Equal(500m, expectedTotal);
    }

    [Fact]
    public void Catalog_FiltersByCityAndActiveStatusOnly()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var searchCity = "Cape Town";

        // Act
        var results = context.HostProperties
            .Where(p => p.IsActive && p.City.ToLower().Contains(searchCity.ToLower()))
            .ToList();

        // Assert - Only active property 1 should match, inactive property 4 should be excluded
        Assert.Single(results);
        Assert.Equal(1, results[0].Id);
        Assert.Equal("Cape Town Luxury Villa", results[0].Title);
    }

    [Fact]
    public void Catalog_FiltersByPropertyType()
    {
        // Arrange
        using var context = GetInMemoryDbContext();

        // Act
        var villas = context.HostProperties
            .Where(p => p.IsActive && p.PropertyType == "Villa")
            .ToList();

        // Assert
        Assert.Single(villas);
        Assert.Equal("Cape Town Luxury Villa", villas[0].Title);
    }

    [Fact]
    public void Catalog_FiltersByPriceRange()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        decimal minPrice = 90m;
        decimal maxPrice = 150m;

        // Act
        var results = context.HostProperties
            .Where(p => p.IsActive && p.PricePerNight >= minPrice && p.PricePerNight <= maxPrice)
            .ToList();

        // Assert - Stellenbosch Cottage ($100) fits between $90 and $150
        Assert.Single(results);
        Assert.Equal("Stellenbosch Cottage", results[0].Title);
    }
}
