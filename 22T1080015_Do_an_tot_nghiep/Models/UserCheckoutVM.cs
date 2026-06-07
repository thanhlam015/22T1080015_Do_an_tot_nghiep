using System.ComponentModel.DataAnnotations;

namespace _22T1080015_Do_an_tot_nghiep.Models
{
    public class UserCheckoutVM
    {
        public int RoomId { get; set; }

        public int AccommodationId { get; set; }

        public string AccommodationName { get; set; } = string.Empty;

        public string RoomType { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public int StayNights { get; set; }

        public int RoomCount { get; set; } = 1;

        public int AdultCount { get; set; } = 2;

        public int ChildCount { get; set; } = 0;

        public int AvailableRooms { get; set; }

        public decimal PricePerNight { get; set; }

        public decimal TotalPrice { get; set; }
        public string? PromotionCode { get; set; }

        public int? PromotionId { get; set; }

        public decimal OriginalTotalPrice { get; set; }

        public decimal DiscountAmount { get; set; }

        public string? PromotionMessage { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        public string? Notes { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string PaymentMethod { get; set; } = "PayAtHotel";
    }

    public class UserBookingSuccessVM
    {
        public int BookingId { get; set; }

        public string AccommodationName { get; set; } = string.Empty;

        public string RoomType { get; set; } = string.Empty;

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public int RoomCount { get; set; }

        public int AdultCount { get; set; }

        public int ChildCount { get; set; }

        public decimal TotalPrice { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = string.Empty;

        public string BookingStatus { get; set; } = string.Empty;

        public string? QrImageUrl { get; set; }

        public string? TransferContent { get; set; }

        public string? BankAccountName { get; set; }

        public string? BankAccountNo { get; set; }
    }
}