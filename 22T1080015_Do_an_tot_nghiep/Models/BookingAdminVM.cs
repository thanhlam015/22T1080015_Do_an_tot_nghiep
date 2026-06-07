namespace _22T1080015_Do_an_tot_nghiep.Models
{
    public class BookingListItemVM
    {
        public int Id { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        public string AccommodationName { get; set; } = string.Empty;

        public string RoomType { get; set; } = string.Empty;

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public int NumberOfRooms { get; set; }

        public int AdultCount { get; set; }

        public int ChildCount { get; set; }

        public decimal TotalPrice { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? PaymentMethod { get; set; }

        public string? PaymentStatus { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class BookingDetailsVM
    {
        public Booking Booking { get; set; } = new Booking();

        public List<Payment> Payments { get; set; } = new List<Payment>();
    }

    public class BookingNotificationItemVM
    {
        public int Id { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string AccommodationName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public decimal TotalPrice { get; set; }
    }

    public class BookingNotificationVM
    {
        public int PendingCount { get; set; }

        public List<BookingNotificationItemVM> Items { get; set; } = new List<BookingNotificationItemVM>();
    }
}