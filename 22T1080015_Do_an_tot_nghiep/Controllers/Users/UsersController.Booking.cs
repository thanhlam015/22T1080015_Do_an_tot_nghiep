using _22T1080015_Do_an_tot_nghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _22T1080015_Do_an_tot_nghiep.Controllers
{
    public partial class UsersController
    {
        // Bookings
        [Authorize(AuthenticationSchemes = "UserCookie", Roles = "Customer")]
        public async Task<IActionResult> Bookings()
        {
            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                return RedirectToAction(nameof(Auth));
            }

            var bookings = await _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r.Accommodation)
                        .ThenInclude(a => a.AccommodationImages)
                .Where(b => b.UserId == user.UserId)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new UserBookingItemVM
                {
                    BookingId = b.Id,
                    AccommodationId = b.Room.AccommodationId,
                    AccommodationName = b.Room.Accommodation.Name,
                    RoomType = b.Room.RoomType,
                    ImageUrl = b.Room.Accommodation.AccommodationImages
                        .OrderByDescending(i => i.IsPrimary)
                        .ThenBy(i => i.SortOrder ?? int.MaxValue)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault(),
                    CheckInDate = b.CheckInDate,
                    CheckOutDate = b.CheckOutDate,
                    TotalPrice = b.TotalPrice,
                    Status = b.Status,
                    PaymentStatus = b.PaymentStatus,
                    CanCancel = b.Status != "Cancelled" &&
                                b.Status != "Completed" &&
                                b.CheckInDate.Date > DateTime.Today,
                    CanReview = b.Status == "Completed"
                })
                .ToListAsync();

            return View(bookings);
        }
        // CancelBooking
        [HttpPost]
        [Authorize(AuthenticationSchemes = "UserCookie", Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id, string? reason)
        {
            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                return RedirectToAction(nameof(Auth));
            }

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(b => b.Id == id && b.UserId == user.UserId);

            if (booking == null)
            {
                return NotFound();
            }

            if (booking.Status == "Completed" || booking.Status == "Cancelled")
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn này.";
                return RedirectToAction(nameof(Bookings));
            }

            if (booking.CheckInDate.Date <= DateTime.Today)
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn đã đến ngày nhận phòng.";
                return RedirectToAction(nameof(Bookings));
            }

            booking.Status = "Cancelled";
            booking.CancelledAt = DateTime.Now;
            booking.CancelReason = string.IsNullOrWhiteSpace(reason)
                ? "Khách hàng hủy đặt phòng."
                : reason.Trim();
            booking.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã hủy đặt phòng thành công.";
            return RedirectToAction(nameof(Bookings));
        }
    }
}