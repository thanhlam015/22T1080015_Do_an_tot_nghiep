using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace _22T1080015_Do_an_tot_nghiep.Models
{
    public class StaffListItemVM
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public string Status { get; set; } = "Active";

        public bool IsLocked { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }



        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FullName))
                {
                    return "AD";
                }

                var words = FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (words.Length == 1)
                {
                    return words[0].Substring(0, 1).ToUpper();
                }

                return (words[0].Substring(0, 1) + words[^1].Substring(0, 1)).ToUpper();
            }
        }
    }

    public class StaffFormVM
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [StringLength(100, ErrorMessage = "Email không được vượt quá 100 ký tự")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được vượt quá 20 ký tự")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn vai trò")]
        [Display(Name = "Vai trò")]
        public string Role { get; set; } = "Staff";

        [Display(Name = "Tải ảnh đại diện mới")]
        public IFormFile? AvatarFile { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public string? AvatarUrl { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Active";

        [Display(Name = "Khóa tài khoản")]
        public bool IsLocked { get; set; }

        [Display(Name = "Mật khẩu")]
        public string? Password { get; set; }

        [Display(Name = "Xác nhận mật khẩu")]
        public string? ConfirmPassword { get; set; }
    }

    public class AdminAccountMenuVM
    {
        public int? UserId { get; set; }

        public string FullName { get; set; } = "Admin TravelBot";

        public string Role { get; set; } = "Admin";

        public string? AvatarUrl { get; set; }

        public bool IsAuthenticated { get; set; }

        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FullName))
                {
                    return "AT";
                }

                var words = FullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (words.Length == 1)
                {
                    return words[0].Substring(0, 1).ToUpper();
                }

                return (words[0].Substring(0, 1) + words[^1].Substring(0, 1)).ToUpper();
            }
        }
    }

    public class AdminProfileVM
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Display(Name = "Số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Display(Name = "Ảnh đại diện hiện tại")]
        public string? AvatarUrl { get; set; }

        [Display(Name = "Tải ảnh đại diện mới")]
        public IFormFile? AvatarFile { get; set; }

        public string Role { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? LastLoginAt { get; set; }
    }

    public class ChangePasswordVM
    {
        [Display(Name = "Mật khẩu hiện tại")]
        public string? CurrentPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự")]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}