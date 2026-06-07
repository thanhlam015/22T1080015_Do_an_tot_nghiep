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
        // Auth
        [AllowAnonymous]
        public async Task<IActionResult> Auth(string? returnUrl = null)
        {
            var user = await GetCurrentUserAsync();

            if (user != null)
            {
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ReturnUrl = returnUrl;
            ViewBag.ActiveTab = "login";

            return View();
        }

        // Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(UserLoginVM vm, string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.ActiveTab = "login";

            if (!ModelState.IsValid)
            {
                return View("Auth");
            }

            var email = vm.Email.Trim();

            var user = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.Email == email &&
                    u.Role == "Customer");

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                return View("Auth");
            }

            if (user.IsLocked || user.Status != "Active")
            {
                ModelState.AddModelError(string.Empty, "Tài khoản đang bị khóa hoặc ngưng hoạt động.");
                return View("Auth");
            }

            bool passwordOk = PasswordHelper.VerifyPassword(user, vm.Password);

            if (!passwordOk)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                return View("Auth");
            }

            var claims = CreateUserClaims(user);

            var identity = new ClaimsIdentity(claims, "UserCookie");
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = vm.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(vm.RememberMe ? 14 : 7)
            };

            await HttpContext.SignInAsync("UserCookie", principal, authProperties);

            user.LastLoginAt = DateTime.Now;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        // Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(UserRegisterVM vm)
        {
            ViewBag.ActiveTab = "register";

            if (vm.Password != vm.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(vm.ConfirmPassword), "Mật khẩu xác nhận không khớp.");
            }

            bool emailExists = await _context.Users.AnyAsync(u => u.Email == vm.Email.Trim());

            if (emailExists)
            {
                ModelState.AddModelError(nameof(vm.Email), "Email này đã được sử dụng.");
            }

            if (!ModelState.IsValid)
            {
                return View("Auth");
            }

            var user = new User
            {
                FullName = vm.FullName.Trim(),
                Email = vm.Email.Trim(),
                PhoneNumber = vm.PhoneNumber.Trim(),
                Role = "Customer",
                Status = "Active",
                IsLocked = false,
                EmailConfirmed = false,
                CreatedAt = DateTime.Now
            };

            user.PasswordHash = PasswordHelper.HashPassword(user, vm.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();


            TempData["SuccessMessage"] = "Đăng ký tài khoản thành công. Bạn có thể đăng nhập ngay.";

            return RedirectToAction(nameof(Auth));
        }

        // Logout
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("UserCookie");

            Response.Cookies.Delete("TravelBotUserAuth");

            HttpContext.Session.Clear();

            TempData["SuccessMessage"] = "Bạn đã đăng xuất thành công.";

            return RedirectToAction(nameof(Index), "Users");
        }

        // CreateUserClaims
        private List<Claim> CreateUserClaims(User user)
        {
            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Name, user.FullName ?? ""),
        new Claim(ClaimTypes.Email, user.Email ?? ""),
        new Claim(ClaimTypes.Role, "Customer")
    };

            if (!string.IsNullOrWhiteSpace(user.AvatarUrl))
            {
                claims.Add(new Claim("AvatarUrl", user.AvatarUrl));
            }

            return claims;
        }

        // RefreshUserCookieAsync
        private async Task RefreshUserCookieAsync(User user)
        {
            var authResult = await HttpContext.AuthenticateAsync("UserCookie");

            var authProperties = authResult.Properties ?? new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
            };

            var identity = new ClaimsIdentity(CreateUserClaims(user), "UserCookie");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("UserCookie", principal, authProperties);
        }

        // GetSafeReturnUrl
        private string GetSafeReturnUrl(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return returnUrl;
            }

            return Url.Action(nameof(Index), "Users") ?? "/Users/Index";
        }
    }
}