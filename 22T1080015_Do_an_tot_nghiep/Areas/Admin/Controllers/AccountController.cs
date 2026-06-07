using _22T1080015_Do_an_tot_nghiep.Helpers;
using _22T1080015_Do_an_tot_nghiep.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace _22T1080015_Do_an_tot_nghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = "CookieAuth", Roles = "Admin,Staff")]
    public class AccountController : Controller
    {
        private readonly DoAnTotNghiepContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccountController(DoAnTotNghiepContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Profile()
        {
            var user = await GetCurrentAdminAsync();

            if (user == null)
            {
                TempData["ErrorMessage"] = "Chưa tìm thấy tài khoản quản trị.";
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }

            var vm = new AdminProfileVM
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(AdminProfileVM vm)
        {
            var user = await GetCurrentAdminAsync();

            if (user == null)
            {
                return NotFound();
            }

            bool emailExists = await _context.Users
                .AnyAsync(u => u.UserId != user.UserId && u.Email == vm.Email.Trim());

            if (emailExists)
            {
                ModelState.AddModelError(nameof(vm.Email), "Email này đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                vm.Role = user.Role;
                vm.CreatedAt = user.CreatedAt;
                vm.LastLoginAt = user.LastLoginAt;
                return View(vm);
            }

            user.FullName = vm.FullName.Trim();
            user.Email = vm.Email.Trim();
            user.PhoneNumber = vm.PhoneNumber.Trim();
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

                    vm.Role = user.Role;
                    vm.CreatedAt = user.CreatedAt;
                    vm.LastLoginAt = user.LastLoginAt;
                    vm.AvatarUrl = user.AvatarUrl;

                    return View(vm);
                }
            }
            else
            {
                user.AvatarUrl = vm.AvatarUrl;
            }
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật thông tin tài khoản thành công.";
            return RedirectToAction(nameof(Profile));
        }

        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM vm)
        {
            var user = await GetCurrentAdminAsync();

            if (user == null)
            {
                return NotFound();
            }

            if (vm.NewPassword != vm.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(vm.ConfirmPassword), "Mật khẩu xác nhận không khớp.");
            }

            if (!string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                bool currentPasswordOk = PasswordHelper.VerifyPassword(user, vm.CurrentPassword ?? "");

                if (!currentPasswordOk)
                {
                    ModelState.AddModelError(nameof(vm.CurrentPassword), "Mật khẩu hiện tại không đúng.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            user.PasswordHash = PasswordHelper.HashPassword(user, vm.NewPassword);
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
            return RedirectToAction(nameof(Profile));
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home", new { area = "Admin" });
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new AdminLoginVM());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginVM vm, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var email = vm.Email.Trim();

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email == email &&
                    (u.Role == "Admin" || u.Role == "Staff"));

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                return View(vm);
            }

            if (user.IsLocked || user.Status != "Active")
            {
                ModelState.AddModelError(string.Empty, "Tài khoản này đang bị khóa hoặc ngưng hoạt động.");
                return View(vm);
            }

            bool passwordOk = PasswordHelper.VerifyPassword(user, vm.Password);

            if (!passwordOk)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                return View(vm);
            }

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Name, user.FullName),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role)
    };

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = vm.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(vm.RememberMe ? 24 * 14 : 8)
            };

            await HttpContext.SignInAsync("CookieAuth", principal, authProperties);

            user.LastLoginAt = DateTime.Now;
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home", new { area = "Admin" });
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");

            return RedirectToAction(nameof(Login), "Account", new { area = "Admin" });
        }

        private async Task<User?> GetCurrentAdminAsync()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdClaim, out int userId))
            {
                return null;
            }

            return await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == userId &&
                    (u.Role == "Admin" || u.Role == "Staff"));
        }
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
                throw new InvalidOperationException("Chỉ cho phép tải ảnh JPG, JPEG, PNG hoặc WEBP.");
            }

            if (file.Length > 2 * 1024 * 1024)
            {
                throw new InvalidOperationException("Dung lượng ảnh không được vượt quá 2MB.");
            }

            var uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");

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

            return $"/uploads/avatars/{fileName}";
        }

        private void DeleteLocalFile(string? fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                return;
            }

            if (!fileUrl.StartsWith("/"))
            {
                return;
            }

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
