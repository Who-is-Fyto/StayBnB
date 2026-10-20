# Staybnb - 15-Phase Development Roadmap

This document outlines the complete 15-phase implementation plan for **Staybnb**, an ASP.NET Core MVC property rental ecosystem based on the system specification in [StaybnbAirbnbClone.md](file:///home/michael/Documents/StayBnB/StaybnbAirbnbClone.md) and the entity architecture diagrams ([DATABSPIC1.png](file:///home/michael/Documents/StayBnB/DATABSPIC1.png), [DATABSPIC2.png](file:///home/michael/Documents/StayBnB/DATABSPIC2.png), [DATABSPIC3.png](file:///home/michael/Documents/StayBnB/DATABSPIC3.png)).

---

## Architecture Overview

Staybnb is engineered around the User Lifecycle, structured roles (`SuperAdmin`, `Admin`, `Host`, `Guest`), strict access control, property management, guest booking workflows, multi-step digital check-ins, internal messaging, notifications, and security auditing.

---

## Phase 1: Project Architecture, NuGet Dependencies & Folder Organization
- **Goal:** Establish the enterprise directory structure, install required EF Core and Identity libraries, and configure foundational web settings.
- **Key Deliverables:**
  - Update [Stay_BnB.csproj](file:///home/michael/Documents/StayBnB/Stay_BnB/Stay_BnB.csproj) with essential packages:
    - `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
    - `Microsoft.EntityFrameworkCore.SqlServer`
    - `Microsoft.EntityFrameworkCore.Tools`
    - `Microsoft.EntityFrameworkCore.Design`
  - Establish solution folder hierarchy:
    - `Data/` (DbContext, Seeders, Configurations)
    - `Models/` (Domain Entities, Enums)
    - `ViewModels/` (Identity, Hosting, Booking, CheckIn, Admin ViewModels)
    - `Services/` (Interfaces and implementations for Logging, Notifications, Storage)
    - `Controllers/` (Account, Properties, Booking, Host, Admin, SuperAdmin, Messages)
    - `Views/` (Organized by controller and shared partials)
  - Configure `appsettings.json` with connection strings and application parameters.

---

## Phase 2: Domain Entity Model Construction & Enums Definition
- **Goal:** Implement the complete 15-entity domain model and associated enumerations according to the database specifications.
- **Key Deliverables:**
  - Define domain enums:
    - `BookingStatus`: Pending, Approved, Rejected, Cancelled, Completed
    - `PaymentStatus`: Pending, Completed, Failed, Refunded
    - `ApplicationStatus`: Pending, Approved, Rejected
    - `CheckInStatus`: Pending, InProgress, Completed, Verified
    - `DocumentStatus`: Uploaded, PendingVerification, Verified, Rejected
    - `ActivityType`: Login, RoleChange, BookingUpdate, PropertyCreated, HostApplication, SecurityEvent
    - `NotificationType`: BookingApproved, BookingRejected, NewBookingRequest, NewMessage, ApplicationStatus, CheckInUpdate, SystemAlert
  - Implement domain models:
    - `ApplicationUser` (extending `IdentityUser`) with `FirstName`, `LastName`, `ProfilePictureUrl`, `CreatedAt`
    - `HostProperty`: Title, Description, PricePerNight, HostId, Address, IsActive, City, PropertyType, CleaningFee, ServiceFee, MaxGuests
    - `Booking`: PropertyId, GuestId, CheckInDate, CheckOutDate, TotalPrice, Status
    - `Payment`: BookingId, Amount, Status, CreatedAt
    - `Review`: PropertyId, ReviewerId, Rating (1-5), Comment, CreatedAt
    - `Amenity`: Name, IconClass
    - `PropertyImage`: PropertyId, ImageUrl, IsCover
    - `WishlistItem`: UserId, PropertyId, CreatedAt
    - `Message`: SenderId, ReceiverId, Content, Timestamp, IsRead
    - `ActivityLog`: UserId, Action, ActivityType, CreatedAt
    - `Notification`: UserId, Title, Type, CreatedAt, IsRead, TargetUrl
    - `HostApplication`: ApplicationUserId, PropertyId, Status, AppliedAt, RejectionReason
    - `CheckInProcess`: PropertyId, Title, StepsJson, CreatedAt
    - `GuestCheckIn`: BookingId, CheckInProcessId, Status, CreatedAt
    - `GuestDocument`: GuestCheckInId, DocumentType, FileName, Status

---

## Phase 3: ApplicationDbContext, Fluent API Relationships & Initial Migrations
- **Goal:** Configure entity mappings, table constraints, and relational integrity in Entity Framework Core.
- **Key Deliverables:**
  - Create `ApplicationDbContext` inheriting from `IdentityDbContext<ApplicationUser>`.
  - Configure key relationships via Fluent API:
    - One-to-Many: `ApplicationUser` -> `HostProperty`
    - One-to-Many: `ApplicationUser` -> `Booking`
    - One-to-Many: `ApplicationUser` -> `Review`
    - One-to-Many: `HostProperty` -> `Booking`
    - One-to-Many: `HostProperty` -> `Review`
    - One-to-One: `Booking` -> `Payment`
    - Many-to-Many: `HostProperty` <-> `Amenity`
    - One-to-Many: `HostProperty` -> `PropertyImage`
    - One-to-One: `HostProperty` -> `CheckInProcess`
    - One-to-One: `Booking` -> `GuestCheckIn`
    - One-to-Many: `GuestCheckIn` -> `GuestDocument`
    - One-to-Many: `ApplicationUser` -> `WishlistItem`
  - Configure cascading delete rules to safeguard financial and historical log data.
  - Generate and apply initial EF Core database migration.

---

## Phase 4: Database Seeder Engine (Roles, SuperAdmin & Master Data)
- **Goal:** Implement the startup seeding pipeline for user roles, default administrative accounts, and baseline catalog data.
- **Key Deliverables:**
  - Implement `RoleSeeder` to establish standard roles:
    - `SuperAdmin`
    - `Admin`
    - `Host`
    - `Guest`
  - Seed default `SuperAdmin` user with secure credentials.
  - Seed standard catalog amenities (WiFi, Free Parking, Kitchen, Air Conditioning, Pool, Dedicated Workspace, Pet Friendly) with FontAwesome icon classes.
  - Wire seeder execution into [Program.cs](file:///home/michael/Documents/StayBnB/Stay_BnB/Program.cs) on application startup.

---

## Phase 5: Authentication, Registration & Mutual Role Enforcement
- **Goal:** Implement secure identity workflows with automatic Guest assignment and strict role exclusivity.
- **Key Deliverables:**
  - Account registration with `FirstName`, `LastName`, `Email`, and `Password`.
  - Ensure all new registrants are automatically assigned the `Guest` role.
  - Login, logout, and profile management views.
  - Implement access control rules:
    - Users operate strictly as either a `Guest` or a `Host` (not both).
    - Apply `[Authorize(Roles = "...")]` attributes across protected endpoints.
    - Prevent Hosts from accessing Guest-only routes and vice-versa.
  - Connect login events to the `ActivityLog` service.

---

## Phase 6: Airbnb-Inspired Responsive UI, Layout & Dynamic Navigation
- **Goal:** Construct a clean, modern user interface mirroring Airbnb aesthetics and adaptive navigation based on active authentication state.
- **Key Deliverables:**
  - Implement custom CSS styling, typography, and card designs in `wwwroot/css/site.css`.
  - Create dynamic navigation bar in `_Layout.cshtml`:
    - Unauthenticated: "Explore", "Become a Host" prompt, "Login", "Register".
    - Guest role: "Explore Listings", "Wishlist", "My Trips", "Become a Host", Messages, Notifications, Account.
    - Host role: "Host Dashboard", "My Properties", "Reservations", "Check-in Rules", Messages, Notifications, Account.
    - Admin / SuperAdmin role: "Admin Portal", "Applications", "User Management", "Activity Audit".
  - Notification badge dropdown and message counter in header.

---

## Phase 7: Property Catalog, Advanced Discovery & Filtering (Guest Experience)
- **Goal:** Enable guests to search, filter, and inspect property listings with an intuitive catalog interface.
- **Key Deliverables:**
  - Property listing index with card grid, price badges, and thumbnail carousels.
  - Search and filter bar:
    - Search by destination / city.
    - Filter by property type (Entire Apartment, Villa, Cabin, Room).
    - Filter by price range (Min/Max price per night).
    - Filter by selected amenities.
  - Detailed property view:
    - Image gallery showcasing all uploaded photos.
    - Host profile summary.
    - Full list of amenities with icons.
    - Nightly price breakdown and interactive booking widget placeholder.
    - Customer reviews and average rating score.

---

## Phase 8: Host Onboarding Flow ("Become a Host" Multi-Step Application)
- **Goal:** Build the onboarding application wizard allowing Guests to apply for Host status by submitting a property.
- **Key Deliverables:**
  - Application form collecting property specifics:
    - Title, Description, Property Type, Address, City.
    - PricePerNight, CleaningFee, ServiceFee, MaxGuests.
    - Amenity multi-selection.
  - Mandatory property image upload:
    - Enforce validation requiring at least one image before submission is allowed.
    - Save uploaded image assets to `wwwroot/uploads/properties/`.
  - Create `HostApplication` record with `Pending` status and create corresponding unlisted `HostProperty`.
  - Issue confirmation notification to applicant and log submission event.

---

## Phase 9: Admin Dashboard & Host Application Review Engine
- **Goal:** Provide administrators with tools to evaluate, approve, or reject incoming host applications with automated role migration.
- **Key Deliverables:**
  - Protected Admin portal (`[Authorize(Roles = "Admin,SuperAdmin")]`).
  - Pending applications list displaying applicant information, property specifics, and uploaded photos.
  - Application decision workflow:
    - **Approval Action:**
      1. Add `Host` role to user.
      2. Remove `Guest` role from user (enforcing role exclusivity).
      3. Set `HostApplication.Status` to `Approved`.
      4. Activate property (`IsActive = true`).
      5. Dispatch notification to the newly approved Host.
      6. Record `RoleChange` in `ActivityLog`.
    - **Rejection Action:**
      1. Set `HostApplication.Status` to `Rejected` with feedback reason.
      2. Keep user in `Guest` role.
      3. Dispatch rejection notice.
      4. Record decision in `ActivityLog`.

---

## Phase 10: SuperAdmin Governance & Administrative Promotion System
- **Goal:** Equip SuperAdmin with exclusive authority to audit users and promote qualified accounts to the Admin role.
- **Key Deliverables:**
  - SuperAdmin control panel (`[Authorize(Roles = "SuperAdmin")]`).
  - User directory view displaying name, email, current roles, registration date, and activity summary.
  - Role promotion action:
    - Promote existing `Guest` to `Admin`.
    - Restrict this capability strictly to the `SuperAdmin` role.
    - Prohibit self-demotion or removal of the root SuperAdmin.
  - Record administrative promotions in `ActivityLog`.

---

## Phase 11: Booking Engine, Price Calculator & Reservation Workflow
- **Goal:** Build the end-to-end reservation system calculating exact pricing and tracking booking states.
- **Key Deliverables:**
  - Interactive booking modal/form on property page:
    - Check-in and check-out date selection with calendar validation.
    - Guest count validation against `MaxGuests`.
    - Verification that requested dates do not overlap with existing confirmed reservations.
  - Transparent price calculation formula:
    `TotalPrice = (PricePerNight x NumberOfNights) + CleaningFee + ServiceFee`
  - Create `Booking` in `Pending` status and generate corresponding `Payment` record.
  - Notify the Host of a new pending booking request.
  - Record booking creation in `ActivityLog`.

---

## Phase 12: Host Management Dashboard & Reservation Controls
- **Goal:** Deliver a management hub for approved Hosts to govern their listings and incoming bookings.
- **Key Deliverables:**
  - Host Dashboard overview:
    - Active listings summary and total revenue metric.
    - Quick toggle for property `IsActive` visibility status.
    - Ability to edit property details and add/remove property images.
  - Reservation management center:
    - View all pending, confirmed, and completed reservations.
    - Actions to `Approve` or `Reject` pending booking requests.
  - Update booking status, trigger notifications to guests, and log status updates.

---

## Phase 13: Digital Check-in Configuration & Guest Document Verification
- **Goal:** Enable hosts to specify arrival procedures and require guests to upload identification documents for stay verification.
- **Key Deliverables:**
  - Host Check-in Configuration (`CheckInProcess`):
    - Host defines check-in steps, entry codes, arrival window, and document requirements.
    - Stored as structured data linked to `HostProperty`.
  - Guest Check-in Wizard (`GuestCheckIn`):
    - Accessible only for approved bookings approaching check-in date.
    - Multi-step check-in interface displaying host instructions.
    - Document upload step: Guest uploads `GuestDocument` (ID or Passport file).
    - Enforce file type and size validation.
  - Host Verification Interface:
    - Host inspects uploaded document and either verifies or requests re-upload.
    - Update `CheckInStatus` to `Completed` / `Verified`.

---

## Phase 14: Communication Engine (Internal Messaging & Notification Center)
- **Goal:** Implement database-driven messaging and real-time-like notifications without external dependencies.
- **Key Deliverables:**
  - Messaging System:
    - Inbox view listing conversation threads between Guests and Hosts.
    - Conversation view with chronological message history.
    - Reply form to send new messages linked to properties and bookings.
    - Unread message tracking with visual indicators.
  - Notification Center:
    - Notification bell dropdown in navbar with unread count badge.
    - Dedicated notifications page with status badges:
      - Booking approved / rejected
      - New booking request received
      - New message received
      - Host application approved / rejected
      - Document verification updates
    - Mark notifications as read upon viewing.

---

## Phase 15: Wishlists, Reviews, Security Audit Logs & Final Verification
- **Goal:** Finalize social engagement features, comprehensive security auditing, and end-to-end system testing.
- **Key Deliverables:**
  - Guest Wishlist:
    - Quick-save heart button on property cards.
    - "My Wishlist" view displaying saved properties.
  - Review and Rating System:
    - Guests with completed bookings can submit a 1 to 5 star rating and comment.
    - Dynamic calculation of property average rating and review tally.
  - System Audit Log Viewer (`[Authorize(Roles = "Admin,SuperAdmin")]`):
    - Tabular view of `ActivityLog` entries.
    - Filter by activity type, date range, or user.
  - End-to-end verification:
    - Automated unit and integration testing.
    - Validate role transitions, mutual exclusion, and route protections.
    - Verify file upload constraints and database transactional consistency.
