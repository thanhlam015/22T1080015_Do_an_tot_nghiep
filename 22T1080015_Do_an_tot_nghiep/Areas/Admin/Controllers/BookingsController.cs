using _22T1080015_Do_an_tot_nghiep.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _22T1080015_Do_an_tot_nghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class BookingsController : Controller
    {
        private readonly DoAnTotNghiepContext _context;

        public BookingsController(DoAnTotNghiepContext context)
        {
            _context = context;
        }

        // GET: Admin/Bookings
        public async Task<IActionResult> Index(
            string? keyword,
            string? status,
            string? paymentStatus,
            DateTime? dateFrom,
            DateTime? dateTo,
            int page = 1)
        {
            int pageSize = 10;

            var query = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                    .ThenInclude(r => r.Accommodation)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                if (int.TryParse(keyword, out int bookingId))
                {
                    query = query.Where(b =>
                        b.Id == bookingId ||
                        (b.FullName != null && b.FullName.Contains(keyword)) ||
                        (b.PhoneNumber != null && b.PhoneNumber.Contains(keyword)) ||
                        (b.Email != null && b.Email.Contains(keyword)) ||
                        b.User.FullName.Contains(keyword));
                }
                else
                {
                    query = query.Where(b =>
                        (b.FullName != null && b.FullName.Contains(keyword)) ||
                        (b.PhoneNumber != null && b.PhoneNumber.Contains(keyword)) ||
                        (b.Email != null && b.Email.Contains(keyword)) ||
                        b.User.FullName.Contains(keyword) ||
                        b.Room.Accommodation.Name.Contains(keyword));
                }
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(b => b.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                query = query.Where(b => b.PaymentStatus == paymentStatus);
            }

            if (dateFrom.HasValue)
            {
                query = query.Where(b => b.CheckInDate >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                var endDate = dateTo.Value.Date.AddDays(1);
                query = query.Where(b => b.CheckInDate < endDate);
            }

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .ThenByDescending(b => b.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BookingListItemVM
                {
                    Id = b.Id,
                    CustomerName = !string.IsNullOrWhiteSpace(b.FullName)
                        ? b.FullName
                        : b.User.FullName,
                    PhoneNumber = b.PhoneNumber ?? b.User.PhoneNumber,
                    Email = b.Email ?? b.User.Email,
                    AccommodationName = b.Room.Accommodation.Name,
                    RoomType = b.Room.RoomType,
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    NumberOfRooms = b.NumberOfRooms,
                    AdultCount = b.AdultCount,
                    ChildCount = b.ChildCount,
                    TotalPrice = b.TotalPrice,
                    Status = b.Status,
                    PaymentMethod = b.PaymentMethod,
                    PaymentStatus = b.PaymentStatus,
                    CreatedAt = b.CreatedAt
                })
                .ToListAsync();

            ViewBag.Keyword = keyword;
            ViewBag.Status = status;
            ViewBag.PaymentStatus = paymentStatus;
            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(bookings);
        }

        // GET: Admin/Bookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var booking = await _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                    .ThenInclude(r => r.Accommodation)
                .Include(b => b.Payments)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == id.Value);

            if (booking == null) return NotFound();

            var vm = new BookingDetailsVM
            {
                Booking = booking,
                Payments = booking.Payments
                    .OrderByDescending(p => p.PaymentDate)
                    .ToList()
            };

            return View(vm);
        }

        // POST: Admin/Bookings/Confirm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            if (booking.Status == "Cancelled")
            {
                TempData["ErrorMessage"] = "Không thể xác nhận đơn đã hủy.";
                return RedirectToAction(nameof(Details), new { id });
            }

            booking.Status = "Confirmed";
            booking.ConfirmedAt = DateTime.Now;
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xác nhận đơn đặt phòng.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/Bookings/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? cancelReason)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            if (booking.Status == "Completed")
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn đã hoàn tất.";
                return RedirectToAction(nameof(Details), new { id });
            }

            booking.Status = "Cancelled";
            booking.CancelledAt = DateTime.Now;
            booking.CancelReason = string.IsNullOrWhiteSpace(cancelReason)
                ? "Đơn đặt phòng bị hủy bởi quản trị viên."
                : cancelReason.Trim();
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã hủy đơn đặt phòng.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/Bookings/MarkPaid/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id, string? paymentMethod)
        {
            var booking = await _context.Bookings
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            booking.PaymentStatus = "Paid";
            booking.PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod)
                ? booking.PaymentMethod ?? "PayAtHotel"
                : paymentMethod.Trim();
            booking.UpdatedAt = DateTime.Now;

            bool hasPaidPayment = booking.Payments.Any(p => p.PaymentStatus == "Paid");

            if (!hasPaidPayment)
            {
                var payment = new Payment
                {
                    BookingId = booking.Id,
                    PaymentMethod = booking.PaymentMethod ?? "PayAtHotel",
                    PaymentStatus = "Paid",
                    Amount = booking.TotalPrice,
                    PaymentDate = DateTime.Now
                };

                _context.Payments.Add(payment);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã cập nhật trạng thái thanh toán.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/Bookings/MarkUnpaid/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkUnpaid(int id)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            booking.PaymentStatus = "Unpaid";
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã chuyển đơn về trạng thái chưa thanh toán.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/Bookings/Complete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(int id)
        {
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            if (booking.Status == "Cancelled")
            {
                TempData["ErrorMessage"] = "Không thể hoàn tất đơn đã hủy.";
                return RedirectToAction(nameof(Details), new { id });
            }

            booking.Status = "Completed";
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã chuyển đơn sang trạng thái hoàn tất.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}