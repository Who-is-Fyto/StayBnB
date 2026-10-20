namespace Stay_BnB.Models.Enums;

public enum BookingStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Cancelled = 3,
    Completed = 4
}

public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Refunded = 3
}

public enum ApplicationStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public enum CheckInStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Verified = 3
}

public enum DocumentStatus
{
    Uploaded = 0,
    PendingVerification = 1,
    Verified = 2,
    Rejected = 3
}

public enum ActivityType
{
    Login = 0,
    RoleChange = 1,
    BookingUpdate = 2,
    PropertyCreated = 3,
    HostApplication = 4,
    SecurityEvent = 5
}

public enum NotificationType
{
    BookingApproved = 0,
    BookingRejected = 1,
    NewBookingRequest = 2,
    NewMessage = 3,
    ApplicationStatus = 4,
    CheckInUpdate = 5,
    SystemAlert = 6
}
