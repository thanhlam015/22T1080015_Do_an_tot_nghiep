namespace _22T1080015_Do_an_tot_nghiep.Models
{
    public class CustomerListItemVM
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public string Status { get; set; } = "Active";

        public bool IsLocked { get; set; }

        public string? LockReason { get; set; }

        public DateTime? LockedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public bool EmailConfirmed { get; set; }

        public int BookingCount { get; set; }

        public decimal TotalSpent { get; set; }

        public int ReviewCount { get; set; }

        public int SavedAccommodationCount { get; set; }

        public DateTime? LastBookingDate { get; set; }

        public string FirstLetter
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FullName))
                {
                    return "?";
                }

                return FullName.Trim().Substring(0, 1).ToUpper();
            }
        }
    }

    public class CustomerDetailsVM
    {
        public User User { get; set; } = new User();

        public int BookingCount { get; set; }

        public decimal TotalSpent { get; set; }

        public int ReviewCount { get; set; }

        public int SavedAccommodationCount { get; set; }

        public List<CustomerBookingHistoryVM> Bookings { get; set; } = new List<CustomerBookingHistoryVM>();

        public List<CustomerReviewHistoryVM> Reviews { get; set; } = new List<CustomerReviewHistoryVM>();

        public List<CustomerSavedAccommodationVM> SavedAccommodations { get; set; } = new List<CustomerSavedAccommodationVM>();
    }

    public class CustomerBookingHistoryVM
    {
        public int BookingId { get; set; }

        public string AccommodationName { get; set; } = string.Empty;

        public string RoomType { get; set; } = string.Empty;

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public decimal TotalPrice { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? PaymentMethod { get; set; }

        public string? PaymentStatus { get; set; }
    }

    public class CustomerReviewHistoryVM
    {
        public int ReviewId { get; set; }

        public string AccommodationName { get; set; } = string.Empty;

        public int? Rating { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class CustomerSavedAccommodationVM
    {
        public int AccommodationId { get; set; }

        public string AccommodationName { get; set; } = string.Empty;

        public DateTime? CreatedAt { get; set; }
    }
}