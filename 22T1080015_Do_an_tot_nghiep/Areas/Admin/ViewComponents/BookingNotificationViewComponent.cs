using _22T1080015_Do_an_tot_nghiep.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _22T1080015_Do_an_tot_nghiep.Areas.Admin.ViewComponents
{
    public class BookingNotificationViewComponent : ViewComponent
    {
        private readonly DoAnTotNghiepContext _context;

        public BookingNotificationViewComponent(DoAnTotNghiepContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var pendingQuery = _context.Bookings
                .Include(b => b.User)
                .Include(b => b.Room)
                    .ThenInclude(r => r.Accommodation)
                .Where(b => b.Status == "Pending");

            var vm = new BookingNotificationVM
            {
                PendingCount = await pendingQuery.CountAsync(),

                Items = await pendingQuery
                    .OrderByDescending(b => b.CreatedAt)
                    .ThenByDescending(b => b.Id)
                    .Take(5)
                    .Select(b => new BookingNotificationItemVM
                    {
                        Id = b.Id,
                        CustomerName = !string.IsNullOrWhiteSpace(b.FullName)
                            ? b.FullName
                            : b.User.FullName,
                        AccommodationName = b.Room.Accommodation.Name,
                        CreatedAt = b.CreatedAt,
                        TotalPrice = b.TotalPrice
                    })
                    .ToListAsync()
            };

            return View(vm);
        }
    }
}