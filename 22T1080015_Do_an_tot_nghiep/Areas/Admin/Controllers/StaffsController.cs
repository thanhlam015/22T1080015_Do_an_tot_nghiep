using _22T1080015_Do_an_tot_nghiep.Helpers;
using _22T1080015_Do_an_tot_nghiep.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _22T1080015_Do_an_tot_nghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class StaffsController : Controller
    {
        private readonly DoAnTotNghiepContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public StaffsController(DoAnTotNghiepContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(string? keyword, string? role, string? status, int page = 1)
        {
            int pageSize = 10;

            var query = _context.Users
                .AsNoTracking()
                .Where(u => u.Role == "Admin" || u.Role == "Staff")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(u =>
                    u.FullName.Contains(keyword) ||
                    u.Email.Contains(keyword) ||
                    u.PhoneNumber.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "Locked")
                {
                    query = query.Where(u => u.IsLocked);
                }
                else
                {
                    query = query.Where(u => !u.IsLocked && u.Status == status);
                }
            }

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var staffs = await query
                .OrderByDescending(u => u.UserId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new StaffListItemVM
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Role = u.Role,
                    AvatarUrl = u.AvatarUrl,
                    Status = u.IsLocked ? "Locked" : u.Status,
                    IsLocked = u.IsLocked,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt
                })
                .ToListAsync();

            ViewBag.Keyword = keyword;
            ViewBag.Role = role;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(staffs);
        }

        public IActionResult Create()
        {
            var vm = new StaffFormVM
            {
                Role = "Staff",
                Status = "Active"
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StaffFormVM vm)
        {
            if (string.IsNullOrWhiteSpace(vm.Password))
            {
                ModelState.AddModelError(nameof(vm.Password), "Vui lòng nhập mật khẩu.");
            }

            if (vm.Password != vm.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(vm.ConfirmPassword), "Mật khẩu xác nhận không khớp.");
            }

            if (vm.Role != "Admin" && vm.Role != "Staff")
            {
                ModelState.AddModelError(nameof(vm.Role), "Vai trò không hợp lệ.");
            }

            bool emailExists = await _context.Users.AnyAsync(u => u.Email == vm.Email.Trim());

            if (emailExists)
            {
                ModelState.AddModelError(nameof(vm.Email), "Email này đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }
            string? avatarUrl = null;

            try
            {
                avatarUrl = await SaveAvatarAsync(vm.AvatarFile);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(vm.AvatarFile), ex.Message);
                return View(vm);
            }
            var user = new User
            {
                FullName = vm.FullName.Trim(),
                Email = vm.Email.Trim(),
                PhoneNumber = vm.PhoneNumber.Trim(),
                Role = vm.Role,
                AvatarUrl = avatarUrl,
                Status = vm.IsLocked ? "Locked" : "Active",
                IsLocked = vm.IsLocked,
                CreatedAt = DateTime.Now,
                EmailConfirmed = true
            };

            user.PasswordHash = PasswordHelper.HashPassword(user, vm.Password!);

            if (vm.IsLocked)
            {
                user.LockedAt = DateTime.Now;
                user.LockReason = "Tài khoản được tạo ở trạng thái khóa.";
            }

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thêm tài khoản nhân viên thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id.Value && (u.Role == "Admin" || u.Role == "Staff"));

            if (user == null) return NotFound();

            var vm = new StaffFormVM
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status,
                IsLocked = user.IsLocked
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, StaffFormVM vm)
        {
            if (id != vm.UserId) return NotFound();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id && (u.Role == "Admin" || u.Role == "Staff"));

            if (user == null) return NotFound();

            if (vm.Role != "Admin" && vm.Role != "Staff")
            {
                ModelState.AddModelError(nameof(vm.Role), "Vai trò không hợp lệ.");
            }

            bool emailExists = await _context.Users
                .AnyAsync(u => u.UserId != id && u.Email == vm.Email.Trim());

            if (emailExists)
            {
                ModelState.AddModelError(nameof(vm.Email), "Email này đã tồn tại.");
            }

            if (!string.IsNullOrWhiteSpace(vm.Password) || !string.IsNullOrWhiteSpace(vm.ConfirmPassword))
            {
                if (vm.Password != vm.ConfirmPassword)
                {
                    ModelState.AddModelError(nameof(vm.ConfirmPassword), "Mật khẩu xác nhận không khớp.");
                }

                if (vm.Password != null && vm.Password.Length < 6)
                {
                    ModelState.AddModelError(nameof(vm.Password), "Mật khẩu phải có ít nhất 6 ký tự.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            user.FullName = vm.FullName.Trim();
            user.Email = vm.Email.Trim();
            user.PhoneNumber = vm.PhoneNumber.Trim();
            user.Role = vm.Role;
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
                    return View(vm);
                }
            }
            else
            {
                user.AvatarUrl = vm.AvatarUrl;
            }
            user.UpdatedAt = DateTime.Now;

            if (vm.IsLocked)
            {
                user.IsLocked = true;
                user.Status = "Locked";

                if (user.LockedAt == null)
                {
                    user.LockedAt = DateTime.Now;
                    user.LockReason = "Tài khoản bị khóa bởi quản trị viên.";
                }
            }
            else
            {
                user.IsLocked = false;
                user.Status = vm.Status == "Inactive" ? "Inactive" : "Active";
                user.LockedAt = null;
                user.LockReason = null;
            }

            if (!string.IsNullOrWhiteSpace(vm.Password))
            {
                user.PasswordHash = PasswordHelper.HashPassword(user, vm.Password);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật tài khoản nhân viên thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(int id, string? returnUrl)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id && (u.Role == "Admin" || u.Role == "Staff"));

            if (user == null) return NotFound();

            if (user.Role == "Admin" && !user.IsLocked)
            {
                int activeAdminCount = await _context.Users.CountAsync(u =>
                    u.Role == "Admin" &&
                    !u.IsLocked &&
                    u.Status == "Active");

                if (activeAdminCount <= 1)
                {
                    TempData["ErrorMessage"] = "Không thể khóa quản trị viên cuối cùng đang hoạt động.";
                    return RedirectBack(returnUrl);
                }
            }

            if (user.IsLocked)
            {
                user.IsLocked = false;
                user.Status = "Active";
                user.LockedAt = null;
                user.LockReason = null;
                user.UpdatedAt = DateTime.Now;

                TempData["SuccessMessage"] = "Đã mở khóa tài khoản.";
            }
            else
            {
                user.IsLocked = true;
                user.Status = "Locked";
                user.LockedAt = DateTime.Now;
                user.LockReason = "Tài khoản bị khóa bởi quản trị viên.";
                user.UpdatedAt = DateTime.Now;

                TempData["SuccessMessage"] = "Đã khóa tài khoản.";
            }

            await _context.SaveChangesAsync();

            return RedirectBack(returnUrl);
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

        private IActionResult RedirectBack(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}