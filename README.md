# Staybnb - Property Rental Web Application

## Student & Project Information
- **Project Name:** Staybnb (Airbnb Clone)
- **Framework:** ASP.NET Core MVC (.NET 10)
- **Database:** Entity Framework Core with SQL Server / SQLite
- **Architecture:** Model-View-Controller (MVC) with ASP.NET Core Identity
- **Repository Path:** `/home/michael/Documents/StayBnB/Stay_BnB`

---

## 1. Project Overview
Staybnb is an online vacation rental and property booking web application built as an MVC project modeled after Airbnb. The system handles the entire user lifecycle, allowing guests to search for places to stay, book dates, calculate prices, complete digital check-in requirements, and apply to become property hosts. 

The application implements role-based access control with four distinct roles:
1. **Guest:** Default role for all newly registered users. Can browse listings, save to wishlists, make bookings, upload check-in documents, message hosts, and submit a host application.
2. **Host:** Approved property owners. Can manage listings, toggle property active/inactive status, accept or reject incoming bookings, set check-in rules, and verify guest check-in documents.
3. **Admin:** System moderators who review and approve or reject host applications, and inspect system activity logs.
4. **SuperAdmin:** Seeded automatically on startup. The only account with permissions to promote guests to administrators and manage platform staff.

A core business rule of this platform is **role exclusivity**: a user can only be either a Guest or a Host at any given time, never both. When an Admin approves a guest's host application, the system automatically removes their Guest role and assigns the Host role.

---

## 2. Core Features

### Guest Features
- **Browse & Search:** Explore active listings with image carousels, pricing, and location details. Filter properties by city, property type, price range, and amenities.
- **Booking Engine:** Choose check-in and check-out dates along with the number of travelers. The system validates availability and calculates total pricing using the required formula:
  `TotalPrice = (PricePerNight x Nights) + CleaningFee + ServiceFee`
- **Guest Check-in Wizard:** Once a booking is approved, guests walk through the host's custom check-in instructions and upload an ID or passport document for verification.
- **Wishlist:** Save favorite properties for later viewing.
- **Reviews & Ratings:** Leave a 1 to 5 star rating with comments after completing a stay.
- **Messaging & Notifications:** Direct messaging with property hosts and an in-app notification center for booking status updates.

### Host Features
- **Become a Host Application:** Guests can apply to list a property by providing details (title, description, price, address, city, amenities) and at least one property image. Applications cannot be submitted without an image.
- **Host Dashboard:** View and manage property listings, toggle `IsActive` visibility, and monitor incoming booking requests.
- **Booking Management:** Review pending booking requests from guests and choose to approve or reject them.
- **Check-in Configuration:** Configure property check-in rules, house manuals, arrival instructions, and required verification documents.
- **Document Verification:** Inspect uploaded guest IDs or passports and mark them as verified or rejected.

### Admin & SuperAdmin Features
- **Host Application Review (Admin):** Review pending property and host applications. Approving an application activates the listing and automatically changes the user from Guest to Host.
- **User Management (SuperAdmin only):** View registered users and promote existing guests to the Admin role.
- **Activity Audit Log:** View logged system events including logins, role changes, booking updates, and security events.

---

## 3. Database & Entity Architecture
The project uses Entity Framework Core with 15 interconnected models and 7 enumerations:

### Models
1. `ApplicationUser` - Extends IdentityUser with first name, last name, profile image, and navigation links.
2. `HostProperty` - Property listings with title, address, city, nightly price, cleaning and service fees, max guests, and active status.
3. `Booking` - Reservation records linking guest, property, dates, guest count, total cost, and status.
4. `Payment` - Tracks payment amount and status for each booking (One-to-One with Booking).
5. `Review` - Guest ratings (1-5) and written feedback for properties.
6. `Amenity` - Master list of property features (WiFi, pool, parking, etc.) with icon classes (Many-to-Many with HostProperty).
7. `PropertyImage` - Image URLs and cover image flags for listings.
8. `WishlistItem` - Saved properties per user.
9. `Message` - In-app messages between guests and hosts.
10. `ActivityLog` - Audit trail tracking user actions, logins, role adjustments, and booking changes.
11. `Notification` - In-app alerts for bookings, messages, and application updates.
12. `HostApplication` - Tracks guest requests to become a host with property details and status.
13. `CheckInProcess` - Host-defined check-in steps and instructions (One-to-One with HostProperty).
14. `GuestCheckIn` - Guest check-in progress for an approved booking (One-to-One with Booking).
15. `GuestDocument` - ID or passport files uploaded during check-in.

### Key Enumerations
- `BookingStatus` (Pending, Approved, Rejected, Cancelled, Completed)
- `PaymentStatus` (Pending, Completed, Failed, Refunded)
- `ApplicationStatus` (Pending, Approved, Rejected)
- `CheckInStatus` (Pending, InProgress, Completed, Verified)
- `DocumentStatus` (Uploaded, PendingVerification, Verified, Rejected)
- `ActivityType` (Login, RoleChange, BookingUpdate, PropertyCreated, HostApplication, SecurityEvent)
- `NotificationType` (BookingApproved, BookingRejected, NewBookingRequest, NewMessage, ApplicationStatus, CheckInUpdate, SystemAlert)

---

## 4. Technology Stack
- **Backend:** C# / ASP.NET Core MVC 10
- **Database ORM:** Entity Framework Core 10 (SQL Server & SQLite support)
- **Identity & Security:** ASP.NET Core Identity with role-based authorization attributes
- **Frontend:** Razor Views (.cshtml), Bootstrap 5, Bootstrap Icons, custom CSS
- **File Storage:** Local uploads folder (`wwwroot/uploads/`) for property photos and guest documents

---

## 5. Getting Started & Setup

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/) installed
- SQL Server (LocalDB, Express, or Docker) OR SQLite (supported out of the box)

### Installation Steps

1. **Clone or navigate to the project directory:**
   ```bash
   cd /home/michael/Documents/StayBnB/Stay_BnB
   ```

2. **Restore NuGet packages:**
   ```bash
   dotnet restore
   ```

3. **Configure the Database in `appsettings.json`:**
   By default, the application is set to use SQLite for easy local development without needing an external database server running:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=StaybnbDb;Trusted_Connection=True;MultipleActiveResultSets=true",
       "SqliteConnection": "Data Source=Staybnb.db"
     },
     "DatabaseProvider": "Sqlite"
   }
   ```
   To use SQL Server instead, change `"DatabaseProvider"` to `"SqlServer"`.

4. **Apply EF Core Migrations:**
   ```bash
   dotnet ef database update
   ```

5. **Run the Application:**
   ```bash
   dotnet run
   ```
   Open your browser and navigate to `https://localhost:5001` or `http://localhost:5000`.

---

## 6. Seeded Test Accounts
On the first application run, the database seeder automatically creates the core roles and a default SuperAdmin account:
- **Role:** SuperAdmin
- **Email:** `superadmin@staybnb.com`
- **Password:** `SuperAdmin123!`

You can log in with this account to promote other users or test administrator workflows. Normal registrations through the Register page are automatically assigned the `Guest` role.

---

## 7. Project Structure
```text
Stay_BnB/
|-- Controllers/          # MVC Controllers (Home, Account, Properties, Bookings, Host, Admin, Messages)
|-- Data/                 # ApplicationDbContext, RoleSeeder, Fluent API configurations
|-- Models/               # 15 Domain entity models
|   |-- Enums/            # System enumerations (AppEnums.cs)
|-- Services/             # Business services (Activity logger, Notifications, File upload)
|-- ViewModels/           # Strongly typed view models for forms and dashboards
|   |-- Account/          # Login, Register, Profile view models
|   |-- Admin/            # Application review and user management models
|   |-- Booking/          # Reservation and price calculation models
|   |-- CheckIn/          # Check-in steps and document upload models
|   |-- Host/             # Property creation and listing management models
|-- Views/                # Razor view templates organized by controller
|   |-- Shared/           # Master layout, navigation bar, and partial views
|-- wwwroot/              # Static files (CSS, JavaScript, Bootstrap)
|   |-- uploads/          # Uploaded property images and guest verification documents
|-- appsettings.json      # Connection strings and application settings
|-- Program.cs            # Dependency injection, middleware pipeline, and seeder hook
`-- Stay_BnB.csproj       # Project dependencies and target framework
```

---

## 8. References & Resources Consulted
- Microsoft Documentation: ASP.NET Core MVC Overview (https://learn.microsoft.com/aspnet/core/mvc/overview)
- Microsoft Documentation: Introduction to Identity on ASP.NET Core (https://learn.microsoft.com/aspnet/core/security/authentication/identity)
- Microsoft Documentation: Entity Framework Core Relationships (https://learn.microsoft.com/ef/core/modeling/relationships)
- Bootstrap 5 Documentation (https://getbootstrap.com/docs/5.3/getting-started/introduction/)
- Airbnb Web Platform (UI and workflow reference for host onboarding and booking layouts)
