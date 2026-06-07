using _22T1080015_Do_an_tot_nghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _22T1080015_Do_an_tot_nghiep.Controllers
{
    public partial class UsersController
    {
        // Reviews
        [Authorize(AuthenticationSchemes = "UserCookie", Roles = "Customer")]
        public async Task<IActionResult> Reviews()
        {
            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                return RedirectToAction(nameof(Auth));
            }

            var reviews = await _context.Reviews
                .Include(r => r.Accommodation)
                .Where(r => r.UserId == user.UserId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new UserReviewItemVM
                {
                    ReviewId = r.Id,
                    AccommodationId = r.AccommodationId,
                    AccommodationName = r.Accommodation.Name,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return View(reviews);
        }
        // CreateReview
        [HttpPost]
        [Authorize(AuthenticationSchemes = "UserCookie", Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReview(UserCreateReviewVM vm)
        {
            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                return RedirectToAction(nameof(Auth));
            }

            bool hasCompletedBooking = await _context.Bookings
                .Include(b => b.Room)
                .AnyAsync(b =>
                    b.UserId == user.UserId &&
                    b.Room.AccommodationId == vm.AccommodationId &&
                    b.Status != "Cancelled" &&
                    b.CheckOutDate <= DateTime.Now);

            if (!hasCompletedBooking)
            {
                TempData["ErrorMessage"] = "Bạn chỉ có thể đánh giá nơi lưu trú sau khi hoàn tất đặt phòng.";
                return RedirectToAction(nameof(Details), new { id = vm.AccommodationId });
            }

            bool reviewExists = await _context.Reviews
                .AnyAsync(r =>
                    r.UserId == user.UserId &&
                    r.AccommodationId == vm.AccommodationId);

            if (reviewExists)
            {
                TempData["ErrorMessage"] = "Bạn đã đánh giá nơi lưu trú này rồi.";
                return RedirectToAction(nameof(Details), new { id = vm.AccommodationId });
            }

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Dữ liệu đánh giá không hợp lệ.";
                return RedirectToAction(nameof(Details), new { id = vm.AccommodationId });
            }

            _context.Reviews.Add(new Review
            {
                UserId = user.UserId,
                AccommodationId = vm.AccommodationId,
                Rating = vm.Rating,
                Comment = vm.Comment,
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            await UpdateAccommodationRating(vm.AccommodationId);

            TempData["SuccessMessage"] = "Gửi đánh giá thành công.";
            return RedirectToAction(nameof(Reviews));
        }
        // DeleteReview
        [HttpPost]
        [Authorize(AuthenticationSchemes = "UserCookie", Roles = "Customer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                return RedirectToAction(nameof(Auth));
            }

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == user.UserId);

            if (review == null)
            {
                return NotFound();
            }

            int accommodationId = review.AccommodationId;

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            await UpdateAccommodationRating(accommodationId);

            TempData["SuccessMessage"] = "Đã xóa đánh giá.";
            return RedirectToAction(nameof(Reviews));
        }
        // UpdateAccommodationRating
        private async Task UpdateAccommodationRating(int accommodationId)
        {
            var accommodation = await _context.Accommodations
                .FirstOrDefaultAsync(a => a.Id == accommodationId);

            if (accommodation == null)
            {
                return;
            }

            var ratings = await _context.Reviews
                .Where(r => r.AccommodationId == accommodationId && r.Rating != null)
                .Select(r => r.Rating!.Value)
                .ToListAsync();

            accommodation.AverageRating = ratings.Any()
                ? Math.Round(ratings.Average(), 1)
                : null;

            accommodation.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }
    }
}