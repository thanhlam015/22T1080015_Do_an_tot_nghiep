using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace _22T1080015_Do_an_tot_nghiep.Models
{
    public class UserLoginVM
    {
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }

    public class UserRegisterVM
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class UserProfileVM
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public IFormFile? AvatarFile { get; set; }
    }

    public class UserChangePasswordVM
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class UserBookingItemVM
    {
        public int BookingId { get; set; }

        public string AccommodationName { get; set; } = string.Empty;

        public string RoomType { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public decimal TotalPrice { get; set; }

        public string Status { get; set; } = string.Empty;

        public string? PaymentStatus { get; set; }

        public bool CanCancel { get; set; }

        public bool CanReview { get; set; }

        public int AccommodationId { get; set; }
    }

    public class UserSavedAccommodationItemVM
    {
        public int AccommodationId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public double? AverageRating { get; set; }

        public DateTime? CreatedAt { get; set; }
    }

    public class UserReviewItemVM
    {
        public int ReviewId { get; set; }
        public int Id { get; set; }
        public int AccommodationId { get; set; }
        public string AccommodationName { get; set; } = string.Empty;
        public string? AccommodationImageUrl { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }

        public int? Rating { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; }
    }

    public class UserCreateReviewVM
    {
        public int AccommodationId { get; set; }

        [Range(1, 5, ErrorMessage = "Số sao phải từ 1 đến 5")]
        public int Rating { get; set; } = 5;

        [StringLength(1000, ErrorMessage = "Đánh giá không được vượt quá 1000 ký tự")]
        public string? Comment { get; set; }
    }
}