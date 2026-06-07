using _22T1080015_Do_an_tot_nghiep.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _22T1080015_Do_an_tot_nghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class CustomersController : Controller
    {
        private readonly DoAnTotNghiepContext _context;

        public CustomersController(DoAnTotNghiepContext context)
        {
            _context = context;
        }

        // GET: Admin/Customers
        public async Task<IActionResult> Index(
            string? keyword,
            string? role,
            string? status,
            int page = 1)
        {
            int pageSize = 10;

            var query = _context.Users
                .AsNoTracking()
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

            var users = await query
                .Select(u => new CustomerListItemVM
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    PhoneNumber = u.PhoneNumber,
                    Role = u.Role,
                    AvatarUrl = u.AvatarUrl,

                    Status = u.IsLocked ? "Locked" : u.Status,
                    IsLocked = u.IsLocked,
                    LockReason = u.LockReason,
                    LockedAt = u.LockedAt,
                    CreatedAt = u.CreatedAt,
                    LastLoginAt = u.LastLoginAt,
                    EmailConfirmed = u.EmailConfirmed,

                    BookingCount = u.Bookings.Count,
                    TotalSpent = u.Bookings.Sum(b => (decimal?)b.TotalPrice) ?? 0,
                    ReviewCount = u.Reviews.Count,
                    SavedAccommodationCount = u.SavedAccommodations.Count,

                    LastBookingDate = u.Bookings
                        .OrderByDescending(b => b.CheckInDate)
                        .Select(b => (DateTime?)b.CheckInDate)
                        .FirstOrDefault()
                })
                .OrderByDescending(u => u.UserId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Keyword = keyword;
            ViewBag.Role = role;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(users);
        }

        // GET: Admin/Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Bookings)
                    .ThenInclude(b => b.Room)
                        .ThenInclude(r => r.Accommodation)
                .Include(u => u.Reviews)
                    .ThenInclude(r => r.Accommodation)
                .Include(u => u.SavedAccommodations)
                    .ThenInclude(s => s.Accommodation)
                .FirstOrDefaultAsync(u => u.UserId == id.Value);

            if (user == null)
            {
                return NotFound();
            }

            var vm = new CustomerDetailsVM
            {
                User = user,
                BookingCount = user.Bookings.Count,
                TotalSpent = user.Bookings.Sum(b => b.TotalPrice),
                ReviewCount = user.Reviews.Count,
                SavedAccommodationCount = user.SavedAccommodations.Count,

                Bookings = user.Bookings
                    .OrderByDescending(b => b.CheckInDate)
                    .Select(b => new CustomerBookingHistoryVM
                    {
                        BookingId = b.Id,
                        AccommodationName = b.Room?.Accommodation?.Name ?? "Không xác định",
                        RoomType = b.Room?.RoomType ?? "Không xác định",
                        CheckInDate = b.CheckInDate,
                        CheckOutDate = b.CheckOutDate,
                        TotalPrice = b.TotalPrice,
                        Status = b.Status,
                        PaymentMethod = b.PaymentMethod,
                        PaymentStatus = b.PaymentStatus
                    })
                    .ToList(),

                Reviews = user.Reviews
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new CustomerReviewHistoryVM
                    {
                        ReviewId = r.Id,
                        AccommodationName = r.Accommodation?.Name ?? "Không xác định",
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt
                    })
                    .ToList(),

                SavedAccommodations = user.SavedAccommodations
                    .OrderByDescending(s => s.CreatedAt)
                    .Select(s => new CustomerSavedAccommodationVM
                    {
                        AccommodationId = s.AccommodationId,
                        AccommodationName = s.Accommodation?.Name ?? "Không xác định",
                        CreatedAt = s.CreatedAt
                    })
                    .ToList()
            };

            return View(vm);
        }

        // POST: Admin/Customers/ToggleLock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(int id, string? lockReason, string? returnUrl)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            if (user.Role == "Admin")
            {
                TempData["ErrorMessage"] = "Không thể khóa tài khoản quản trị viên.";
                return RedirectBack(returnUrl);
            }

            if (user.IsLocked)
            {
                user.IsLocked = false;
                user.Status = "Active";
                user.LockReason = null;
                user.LockedAt = null;
                user.UpdatedAt = DateTime.Now;

                TempData["SuccessMessage"] = "Đã mở khóa tài khoản khách hàng.";
            }
            else
            {
                user.IsLocked = true;
                user.Status = "Locked";
                user.LockReason = string.IsNullOrWhiteSpace(lockReason)
                    ? "Tài khoản bị khóa bởi quản trị viên."
                    : lockReason.Trim();
                user.LockedAt = DateTime.Now;
                user.UpdatedAt = DateTime.Now;

                TempData["SuccessMessage"] = "Đã khóa tài khoản khách hàng.";
            }

            await _context.SaveChangesAsync();

            return RedirectBack(returnUrl);
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