using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Stay_BnB.Models;

namespace Stay_BnB.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<HostProperty> HostProperties => Set<HostProperty>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<HostApplication> HostApplications => Set<HostApplication>();
    public DbSet<CheckInProcess> CheckInProcesses => Set<CheckInProcess>();
    public DbSet<GuestCheckIn> GuestCheckIns => Set<GuestCheckIn>();
    public DbSet<GuestDocument> GuestDocuments => Set<GuestDocument>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Host and Property relationship
        builder.Entity<HostProperty>()
            .HasOne(p => p.Host)
            .WithMany(u => u.HostProperties)
            .HasForeignKey(p => p.HostId)
            .OnDelete(DeleteBehavior.Restrict);

        // Booking relationships
        builder.Entity<Booking>()
            .HasOne(b => b.Property)
            .WithMany(p => p.Bookings)
            .HasForeignKey(b => b.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Booking>()
            .HasOne(b => b.Guest)
            .WithMany(u => u.Bookings)
            .HasForeignKey(b => b.GuestId)
            .OnDelete(DeleteBehavior.Restrict);

        // One to one: Booking and Payment
        builder.Entity<Payment>()
            .HasOne(p => p.Booking)
            .WithOne(b => b.Payment)
            .HasForeignKey<Payment>(p => p.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        // Review relationships
        builder.Entity<Review>()
            .HasOne(r => r.Property)
            .WithMany(p => p.Reviews)
            .HasForeignKey(r => r.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Review>()
            .HasOne(r => r.Reviewer)
            .WithMany(u => u.Reviews)
            .HasForeignKey(r => r.ReviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Many to many: HostProperty and Amenity
        builder.Entity<HostProperty>()
            .HasMany(p => p.Amenities)
            .WithMany(a => a.Properties);

        // One to many: HostProperty and PropertyImage
        builder.Entity<PropertyImage>()
            .HasOne(pi => pi.Property)
            .WithMany(p => p.Images)
            .HasForeignKey(pi => pi.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        // One to one: HostProperty and CheckInProcess
        builder.Entity<CheckInProcess>()
            .HasOne(c => c.Property)
            .WithOne(p => p.CheckInProcess)
            .HasForeignKey<CheckInProcess>(c => c.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        // One to one: Booking and GuestCheckIn
        builder.Entity<GuestCheckIn>()
            .HasOne(g => g.Booking)
            .WithOne(b => b.GuestCheckIn)
            .HasForeignKey<GuestCheckIn>(g => g.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<GuestCheckIn>()
            .HasOne(g => g.CheckInProcess)
            .WithMany(c => c.GuestCheckIns)
            .HasForeignKey(g => g.CheckInProcessId)
            .OnDelete(DeleteBehavior.SetNull);

        // One to many: GuestCheckIn and GuestDocument
        builder.Entity<GuestDocument>()
            .HasOne(d => d.GuestCheckIn)
            .WithMany(g => g.GuestDocuments)
            .HasForeignKey(d => d.GuestCheckInId)
            .OnDelete(DeleteBehavior.Cascade);

        // WishlistItem unique combination
        builder.Entity<WishlistItem>()
            .HasIndex(w => new { w.UserId, w.PropertyId })
            .IsUnique();

        builder.Entity<WishlistItem>()
            .HasOne(w => w.User)
            .WithMany(u => u.WishlistItems)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WishlistItem>()
            .HasOne(w => w.Property)
            .WithMany(p => p.WishlistItems)
            .HasForeignKey(w => w.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Message relationships (restrict delete to prevent cascade cycle)
        builder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Message>()
            .HasOne(m => m.Receiver)
            .WithMany(u => u.ReceivedMessages)
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);

        // ActivityLog user link
        builder.Entity<ActivityLog>()
            .HasOne(a => a.User)
            .WithMany(u => u.ActivityLogs)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Notification user link
        builder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // HostApplication relationships
        builder.Entity<HostApplication>()
            .HasOne(ha => ha.ApplicationUser)
            .WithMany(u => u.HostApplications)
            .HasForeignKey(ha => ha.ApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<HostApplication>()
            .HasOne(ha => ha.Property)
            .WithMany(p => p.HostApplications)
            .HasForeignKey(ha => ha.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure decimals
        builder.Entity<HostProperty>().Property(p => p.PricePerNight).HasPrecision(18, 2);
        builder.Entity<HostProperty>().Property(p => p.CleaningFee).HasPrecision(18, 2);
        builder.Entity<HostProperty>().Property(p => p.ServiceFee).HasPrecision(18, 2);
        builder.Entity<Booking>().Property(b => b.TotalPrice).HasPrecision(18, 2);
        builder.Entity<Payment>().Property(p => p.Amount).HasPrecision(18, 2);
    }
}
