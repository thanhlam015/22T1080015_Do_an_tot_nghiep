using _22T1080015_Do_an_tot_nghiep.Helpers;
using _22T1080015_Do_an_tot_nghiep.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace _22T1080015_Do_an_tot_nghiep.Controllers
{
    public partial class UsersController
    {
        // Profile GET
        [Authorize(AuthenticationSchemes = "UserCookie", Roles = "Customer")]
        public async Task<IActionResult> Profile()
        {
            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                return RedirectToAction(nameof(Auth));
            }

            var vm = new UserProfileVM
            {
                UserId = user.UserId,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                AvatarUrl = user.AvatarUrl
            };

            return View(vm);
        }
        // Profile POST
        [HttpPost]
        [Authorize(AuthenticationSchemes = "UserCookie", Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(UserProfileVM vm)
        {
            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                return RedirectToAction(nameof(Auth));
            }

            if (!ModelState.IsValid)
            {
                vm.Email = user.Email;
                vm.AvatarUrl = user.AvatarUrl;
                return View(vm);
            }

            user.FullName = vm.FullName.Trim();
            user.PhoneNumber = vm.PhoneNumber.Trim();
            user.UpdatedAt = DateTime.Now;

            if (vm.AvatarFile != null && vm.AvatarFile.Length > 0)
            {
                try
                {
                    var oldAvatarUrl = user.AvatarUrl;
                    user.AvatarUrl = await SaveAvatarAsync(vm.AvatarFile);
                    DeleteLocalFile(oldAvatarUrl);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(nameof(vm.AvatarFile), ex.Message);
                    vm.Email = user.Email;
                    vm.AvatarUrl = user.AvatarUrl;
                    return View(vm);
                }
            }

            await _context.SaveChangesAsync();
            await RefreshUserCookieAsync(user);

            TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công.";
            return RedirectToAction(nameof(Profile));
        }
        // ChangePassword
        [HttpPost]
        [Authorize(AuthenticationSchemes = "UserCookie", Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(UserChangePasswordVM vm)
        {
            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                return RedirectToAction(nameof(Auth));
            }

            if (vm.NewPassword != vm.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(vm.ConfirmPassword), "Mật khẩu xác nhận không khớp.");
            }

            bool currentPasswordOk = PasswordHelper.VerifyPassword(user, vm.CurrentPassword);

            if (!currentPasswordOk)
            {
                ModelState.AddModelError(nameof(vm.CurrentPassword), "Mật khẩu hiện tại không đúng.");
            }

            if (!ModelState.IsValid)
            {
                var profileVm = new UserProfileVM
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    PhoneNumber = user.PhoneNumber,
                    Email = user.Email,
                    AvatarUrl = user.AvatarUrl
                };

                ViewBag.ChangePasswordErrors = ModelState;
                return View("Profile", profileVm);
            }

            user.PasswordHash = PasswordHelper.HashPassword(user, vm.NewPassword);
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
            return RedirectToAction(nameof(Profile));
        }

        // GetCurrentUserAsync
        private async Task<User?> GetCurrentUserAsync()
        {
            var authResult = await HttpContext.AuthenticateAsync("UserCookie");

            if (!authResult.Succeeded || authResult.Principal == null)
            {
                return null;
            }

            var userIdClaim = authResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return null;
            }

            return await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == userId &&
                    u.Role == "Customer" &&
                    u.Status == "Active" &&
                    !u.IsLocked);
        }
        // SaveAvatarAsync
        private async Task<string?> SaveAvatarAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Chỉ cho phép ảnh JPG, JPEG, PNG hoặc WEBP.");
            }

            if (file.Length > 2 * 1024 * 1024)
            {
                throw new InvalidOperationException("Dung lượng ảnh không được vượt quá 2MB.");
            }

            var uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "users");

            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/uploads/users/{fileName}";
        }
        // DeleteLocalFile
        private void DeleteLocalFile(string? fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return;
            if (!fileUrl.StartsWith("/")) return;

            var relativePath = fileUrl
                .TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}