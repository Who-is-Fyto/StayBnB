Staybnb - Airbnb Clone

Project Overview
You are tasked with building Staybnb, a managed property rental ecosystem inspired by the core functionality of Airbnb using ASP.NET Core MVC along with EF Core and SQL Server. Your goal is to replicate the seamless transition between being a traveler and a lodging provider. The heart of this project is the User Lifecycle: managing how a user explores the platform as a Guest, applies to become a Host, and how Administrators maintain the quality and security of the marketplace. For UI/UX and feature flow inspiration, you are encouraged to study the Airbnb website – specifically their hosting application flow and guest booking dashboards.

Project Requirements
1. Identity & Role Evolution
	• Initial State: All new registrants are automatically assigned the Guest role.
	• SuperAdmin Seeding: The RoleSeeder must create a "SuperAdmin" on startup. This user is the only one capable of promoting Guests to the Admin role.
	• The Transition: When an Admin approves a HostApplication, the system must programmatically:
		1. Add the Host role to the user.
		2. Remove the Guest role (In StayHub, a user is either a Guest or a Host, not both).
	• Access Control: Ensure strict use of [Authorize(Roles = "RoleName")] to protect dashboard routes and ensure a Host cannot access Guest-only pages and vice-versa.

2. The Hosting Workflow (Host Role)
	• The Application: Drawing inspiration from Airbnb’s "Become a Host" flow, Guests apply by providing property details (Title, Description, Price, Address) and at least one PropertyImage. An application is considered incomplete and cannot be submitted without an image.
	• Host Dashboard: Once approved, the UI should shift to a Host-centric view where they can:
		o Manage property listings and toggle IsActive status.
		o Review pending Booking requests (Approve/Reject).
		o Configure the CheckInProcess (Defining the rules and required documents for their specific property).
		
3. The Guest Experience (Guest Role)
	• Property Discovery: Browse active listings with a clean, "Airbnb-style" gallery. Include filtering by city or property type.
	• Booking Engine: Guests select dates and number of travelers. The system must calculate: TotalPrice = (PricePerNight x Nights) + CleaningFee + ServiceFee
	• Guest Check-in: For approved bookings, guests must complete a multi-step workflow, including uploading a GuestDocument (ID or Passport) for the Host to verify.
	
4. Administrative Oversight (Admin Role)
	• Admin Dashboard: A central command center to:
		o Application Review: Approve or reject new Host/Property applications.
		o User Management: (SuperAdmin only) Find existing Guests and promote them to the Admin role.
		o System Audit: Monitor the ActivityLog to track security actions and significant system events
		
5. Communication & Logging
	• Messaging: A database-backed messaging system. While not real-time (no SignalR required), users should have an inbox to read and reply to queries.
	• Notifications: A notification center (similar to the Airbnb bell icon) showing status updates like "Booking Approved" or "New Message."
	• Activity Logging: Use the ActivityLog model to track every login, role change, and booking status update for transparency.


