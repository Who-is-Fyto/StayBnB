using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stay_BnB.Models;
using Stay_BnB.Models.Enums;

namespace Stay_BnB.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Apply any pending migrations automatically on startup
        await context.Database.MigrateAsync();

        // 1. Seed Roles
        string[] roles = { "SuperAdmin", "Admin", "Host", "Guest" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Seed SuperAdmin user
        var superAdminEmail = configuration["StaybnbSettings:SuperAdminEmail"] ?? "superadmin@staybnb.com";
        var superAdminUser = await userManager.FindByEmailAsync(superAdminEmail);
        if (superAdminUser == null)
        {
            superAdminUser = new ApplicationUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                FirstName = "Super",
                LastName = "Admin",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(superAdminUser, "SuperAdmin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdminUser, "SuperAdmin");

                context.ActivityLogs.Add(new ActivityLog
                {
                    UserId = superAdminUser.Id,
                    Action = "System Initialized",
                    ActivityType = ActivityType.SecurityEvent,
                    Details = "SuperAdmin account seeded on application startup",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // 3. Seed Sample Admin
        var adminEmail = "admin@staybnb.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Alice",
                LastName = "Moderator",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // 4. Seed Sample Host
        var hostEmail = "host@staybnb.com";
        var hostUser = await userManager.FindByEmailAsync(hostEmail);
        if (hostUser == null)
        {
            hostUser = new ApplicationUser
            {
                UserName = hostEmail,
                Email = hostEmail,
                FirstName = "David",
                LastName = "Miller",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(hostUser, "Host123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(hostUser, "Host");
            }
        }

        // 5. Seed Sample Guest
        var guestEmail = "guest@staybnb.com";
        var guestUser = await userManager.FindByEmailAsync(guestEmail);
        if (guestUser == null)
        {
            guestUser = new ApplicationUser
            {
                UserName = guestEmail,
                Email = guestEmail,
                FirstName = "Sarah",
                LastName = "Jenkins",
                EmailConfirmed = true,
                CreatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(guestUser, "Guest123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(guestUser, "Guest");
            }
        }

        // 6. Seed Amenities
        if (!await context.Amenities.AnyAsync())
        {
            var amenities = new List<Amenity>
            {
                new() { Name = "Fast WiFi", IconClass = "bi bi-wifi" },
                new() { Name = "Free Parking", IconClass = "bi bi-p-circle" },
                new() { Name = "Equipped Kitchen", IconClass = "bi bi-egg-fried" },
                new() { Name = "Air Conditioning", IconClass = "bi bi-snow" },
                new() { Name = "Dedicated Workspace", IconClass = "bi bi-laptop" },
                new() { Name = "Swimming Pool", IconClass = "bi bi-water" },
                new() { Name = "Pet Friendly", IconClass = "bi bi-heart" },
                new() { Name = "HD TV", IconClass = "bi bi-tv" },
                new() { Name = "Washer & Dryer", IconClass = "bi bi-droplet" },
                new() { Name = "Self Check-in", IconClass = "bi bi-key" }
            };

            context.Amenities.AddRange(amenities);
            await context.SaveChangesAsync();
        }

        // 7. Seed Sample Properties
        hostUser ??= await userManager.FindByEmailAsync(hostEmail);
        if (!await context.HostProperties.AnyAsync() && hostUser != null)
        {
            var wifi = await context.Amenities.FirstOrDefaultAsync(a => a.Name == "Fast WiFi");
            var pool = await context.Amenities.FirstOrDefaultAsync(a => a.Name == "Swimming Pool");
            var parking = await context.Amenities.FirstOrDefaultAsync(a => a.Name == "Free Parking");
            var ac = await context.Amenities.FirstOrDefaultAsync(a => a.Name == "Air Conditioning");
            var kitchen = await context.Amenities.FirstOrDefaultAsync(a => a.Name == "Equipped Kitchen");

            var prop1 = new HostProperty
            {
                Title = "Modern Oceanfront Villa with Infinity Pool",
                Description = "Enjoy panoramic Atlantic ocean views from this modern designer villa. Features an infinity pool, spacious sun deck, and gourmet kitchen.",
                Address = "12 Victoria Road, Camps Bay",
                City = "Cape Town",
                PropertyType = "Villa",
                PricePerNight = 185.00m,
                CleaningFee = 35.00m,
                ServiceFee = 20.00m,
                MaxGuests = 6,
                IsActive = true,
                HostId = hostUser.Id,
                CreatedAt = DateTime.UtcNow
            };

            if (wifi != null) prop1.Amenities.Add(wifi);
            if (pool != null) prop1.Amenities.Add(pool);
            if (parking != null) prop1.Amenities.Add(parking);
            if (ac != null) prop1.Amenities.Add(ac);

            prop1.Images.Add(new PropertyImage
            {
                ImageUrl = "https://images.unsplash.com/photo-1580587771525-78b9dba3b914?auto=format&fit=crop&w=1200&q=80",
                IsCover = true
            });
            prop1.Images.Add(new PropertyImage
            {
                ImageUrl = "https://images.unsplash.com/photo-1512917774080-9991f1c4c750?auto=format&fit=crop&w=1200&q=80",
                IsCover = false
            });

            // Check-in process for property 1
            prop1.CheckInProcess = new CheckInProcess
            {
                Title = "Self Check-in with Smart Lock",
                StepsJson = "[{\"step\":1,\"instruction\":\"Arrive after 14:00 and use keypad code 4821.\"},{\"step\":2,\"instruction\":\"Collect keys and parking remote from entrance console.\"},{\"step\":3,\"instruction\":\"Upload guest ID for security clearance.\"}]",
                CreatedAt = DateTime.UtcNow
            };

            var prop2 = new HostProperty
            {
                Title = "Cozy Loft in Central Historic District",
                Description = "Charming studio loft located in the center of town. Walking distance to artisan cafes, bakeries, and public transit.",
                Address = "45 Church Street",
                City = "Stellenbosch",
                PropertyType = "Apartment",
                PricePerNight = 75.00m,
                CleaningFee = 20.00m,
                ServiceFee = 10.00m,
                MaxGuests = 2,
                IsActive = true,
                HostId = hostUser.Id,
                CreatedAt = DateTime.UtcNow
            };

            if (wifi != null) prop2.Amenities.Add(wifi);
            if (kitchen != null) prop2.Amenities.Add(kitchen);
            if (ac != null) prop2.Amenities.Add(ac);

            prop2.Images.Add(new PropertyImage
            {
                ImageUrl = "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=1200&q=80",
                IsCover = true
            });

            prop2.CheckInProcess = new CheckInProcess
            {
                Title = "Keybox Lockbox Check-in",
                StepsJson = "[{\"step\":1,\"instruction\":\"Locate lockbox next to front door.\"},{\"step\":2,\"instruction\":\"Enter code 1994 to retrieve front gate key.\"}]",
                CreatedAt = DateTime.UtcNow
            };

            context.HostProperties.AddRange(prop1, prop2);
            await context.SaveChangesAsync();
        }

        await context.SaveChangesAsync();
    }
}
