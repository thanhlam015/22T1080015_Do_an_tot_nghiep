using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _22T1080015_Do_an_tot_nghiep.Models;

namespace _22T1080015_Do_an_tot_nghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class HomeController : Controller
    {
        private readonly DoAnTotNghiepContext _context;

        public HomeController(DoAnTotNghiepContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Thống kê cơ bản
            ViewBag.TotalAccommodations = await _context.Accommodations.CountAsync();
            ViewBag.TotalUsers = await _context.Users.CountAsync();
            ViewBag.TotalBookings = await _context.Bookings.CountAsync();

            // 2. Thống kê về AI
            ViewBag.TotalAIResponses = await _context.ChatMessages.CountAsync(m => m.SenderRole == "Bot");

            // 3. Lấy 5 đơn Đặt phòng mới nhất để truyền vào View(Model)
            var recentBookings = await _context.Bookings
                .Include(b => b.User)
                .OrderByDescending(b => b.CheckInDate)
                .Take(5)
                .ToListAsync();

            // 4. Lấy 5 Đánh giá (Review) mới nhất truyền qua ViewBag
            // LƯU Ý: Thay 'Reviews', 'CreatedAt', 'Accommodation' bằng tên cột chuẩn trong file Model của bạn
            var recentReviews = await _context.Reviews
                .Include(r => r.User) // Lấy tên người đánh giá
                .Include(r => r.Accommodation) // Lấy tên khách sạn (nếu có liên kết)
                .OrderByDescending(r => r.CreatedAt) // Sắp xếp theo ngày đánh giá mới nhất
                .Take(5)
                .ToListAsync();
            ViewBag.TopBotQuestions = await _context.BotQuestionLogs
                .Where(q => q.IsInScope)
                .GroupBy(q => q.NormalizedQuestion)
                .Select(g => new
                {
                    Question = g.Key,
                    Count = g.Count(),
                    LastAskedAt = g.Max(x => x.CreatedAt)
                })
                .OrderByDescending(x => x.Count)
                .ThenByDescending(x => x.LastAskedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentReviews = recentReviews;

            return View(recentBookings);
        }
    }
}