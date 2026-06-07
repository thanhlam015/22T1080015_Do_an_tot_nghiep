using _22T1080015_Do_an_tot_nghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _22T1080015_Do_an_tot_nghiep.Controllers
{
    public partial class UsersController
    {
        // Like
        [Authorize(AuthenticationSchemes = "UserCookie", Roles = "Customer")]
        public async Task<IActionResult> Like()
        {
            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                return RedirectToAction(nameof(Auth));
            }

            var saved = await _context.SavedAccommodations
                .Include(s => s.Accommodation)
                    .ThenInclude(a => a.AccommodationImages)
                .Where(s => s.UserId == user.UserId)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new UserSavedAccommodationItemVM
                {
                    AccommodationId = s.AccommodationId,
                    Name = s.Accommodation.Name,
                    Address = s.Accommodation.Address,
                    AverageRating = s.Accommodation.AverageRating,
                    CreatedAt = s.CreatedAt,
                    ImageUrl = s.Accommodation.AccommodationImages
                        .OrderByDescending(i => i.IsPrimary)
                        .ThenBy(i => i.SortOrder ?? int.MaxValue)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return View(saved);
        }
        // ToggleLike
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ToggleLike(int accommodationId, string? returnUrl)
        {
            var safeReturnUrl = GetSafeReturnUrl(returnUrl);

            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                TempData["InfoMessage"] = "Vui lòng đăng nhập để lưu nơi lưu trú.";
                return RedirectToAction(nameof(Auth), new { returnUrl = safeReturnUrl });
            }

            var existing = await _context.SavedAccommodations
                .FirstOrDefaultAsync(s =>
                    s.UserId == user.UserId &&
                    s.AccommodationId == accommodationId);

            if (existing == null)
            {
                _context.SavedAccommodations.Add(new SavedAccommodation
                {
                    UserId = user.UserId,
                    AccommodationId = accommodationId,
                    CreatedAt = DateTime.Now
                });

                TempData["SuccessMessage"] = "Đã lưu nơi lưu trú.";
            }
            else
            {
                _context.SavedAccommodations.Remove(existing);

                TempData["SuccessMessage"] = "Đã bỏ lưu nơi lưu trú.";
            }

            await _context.SaveChangesAsync();

            return Redirect(safeReturnUrl);
        }
    }
}