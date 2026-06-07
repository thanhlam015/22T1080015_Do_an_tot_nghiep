using _22T1080015_Do_an_tot_nghiep.Models;
using Microsoft.AspNetCore.Identity;

namespace _22T1080015_Do_an_tot_nghiep.Helpers
{
    public static class PasswordHelper
    {
        public static string HashPassword(User user, string password)
        {
            // Không mã hóa nữa, lưu trực tiếp password vào cột PasswordHash.
            // Giữ tên hàm HashPassword để khỏi phải sửa nhiều controller.
            return password ?? string.Empty;
        }

        public static bool VerifyPassword(User user, string password)
        {
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                return false;
            }

            if (password == null)
            {
                return false;
            }

            // Kiểm tra password dạng plain text mới.
            if (user.PasswordHash == password)
            {
                return true;
            }

            // Hỗ trợ các tài khoản cũ đã từng được mã hóa bằng PasswordHasher.
            // Nhờ đoạn này, admin/user cũ vẫn có thể đăng nhập nếu password trong DB đang là hash.
            try
            {
                var hasher = new PasswordHasher<User>();
                var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);

                return result == PasswordVerificationResult.Success ||
                       result == PasswordVerificationResult.SuccessRehashNeeded;
            }
            catch
            {
                return false;
            }
        }
    }
}