using _22T1080015_Do_an_tot_nghiep.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace _22T1080015_Do_an_tot_nghiep.Areas.Admin.ViewComponents
{
    public class AdminAccountMenuViewComponent : ViewComponent
    {
        private readonly DoAnTotNghiepContext _context;

        public AdminAccountMenuViewComponent(DoAnTotNghiepContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var vm = new AdminAccountMenuVM();

            if (UserClaimsPrincipal?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

                if (int.TryParse(userIdClaim, out int userId))
                {
                    var user = await _context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.UserId == userId);

                    if (user != null)
                    {
                        vm.UserId = user.UserId;
                        vm.FullName = user.FullName;
                        vm.Role = user.Role;
                        vm.AvatarUrl = user.AvatarUrl;
                        vm.IsAuthenticated = true;
                    }
                }
            }

            if (vm.UserId == null)
            {
                var fallbackAdmin = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Role == "Admin" || u.Role == "Staff")
                    .OrderBy(u => u.Role == "Admin" ? 0 : 1)
                    .ThenBy(u => u.UserId)
                    .FirstOrDefaultAsync();

                if (fallbackAdmin != null)
                {
                    vm.UserId = fallbackAdmin.UserId;
                    vm.FullName = fallbackAdmin.FullName;
                    vm.Role = fallbackAdmin.Role;
                    vm.AvatarUrl = fallbackAdmin.AvatarUrl;
                }
            }

            return View(vm);
        }
    }
}