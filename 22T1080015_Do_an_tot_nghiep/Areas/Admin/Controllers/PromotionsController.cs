using _22T1080015_Do_an_tot_nghiep.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace _22T1080015_Do_an_tot_nghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class PromotionsController : Controller
    {
        private readonly DoAnTotNghiepContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PromotionsController(DoAnTotNghiepContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index(
            string? keyword,
            string? status,
            string? discountType,
            int page = 1)
        {
            int pageSize = 10;
            DateTime now = DateTime.Now;

            var query = _context.Promotions
                .Include(p => p.PromotionAccommodations)
                    .ThenInclude(pa => pa.Accommodation)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(p =>
                    p.Code.Contains(keyword) ||
                    p.Title.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(discountType))
            {
                query = query.Where(p => p.DiscountType == discountType);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "Active")
                {
                    query = query.Where(p =>
                        p.Status == "Active" &&
                        p.StartDate <= now &&
                        p.EndDate >= now);
                }
                else if (status == "Scheduled")
                {
                    query = query.Where(p =>
                        p.Status == "Active" &&
                        p.StartDate > now);
                }
                else if (status == "Expired")
                {
                    query = query.Where(p => p.EndDate < now);
                }
                else if (status == "Inactive")
                {
                    query = query.Where(p => p.Status == "Inactive");
                }
            }

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var promotions = await query
                .OrderByDescending(p => p.CreatedAt)
                .ThenByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Keyword = keyword;
            ViewBag.Status = status;
            ViewBag.DiscountType = discountType;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(promotions);
        }

        public async Task<IActionResult> Create()
        {
            var vm = new PromotionVM
            {
                DiscountType = "Percent",
                DiscountValue = 10,
                MinBookingAmount = 0,
                PerUserLimit = 1,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(7),
                Status = "Active"
            };

            await LoadPromotionFormData(vm);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PromotionVM vm)
        {
            NormalizePromotionVM(vm);
            await ValidatePromotionVM(vm);

            if (!ModelState.IsValid)
            {
                await LoadPromotionFormData(vm);
                return View(vm);
            }

            string? bannerUrl = null;

            try
            {
                bannerUrl = await SaveBannerAsync(vm.BannerFile);
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(nameof(vm.BannerFile), ex.Message);
                await LoadPromotionFormData(vm);
                return View(vm);
            }

            var promotion = new Promotion
            {
                Code = vm.Code,
                Title = vm.Title.Trim(),
                Description = vm.Description,
                DiscountType = vm.DiscountType,
                DiscountValue = vm.DiscountValue,
                MaxDiscountAmount = vm.MaxDiscountAmount,
                MinBookingAmount = vm.MinBookingAmount,
                StartDate = vm.StartDate,
                EndDate = vm.EndDate,
                UsageLimit = vm.UsageLimit,
                PerUserLimit = vm.PerUserLimit,
                BannerImageUrl = bannerUrl,
                Status = vm.Status,
                CreatedAt = DateTime.Now,
                CreatedByUserId = GetCurrentUserId()
            };

            if (vm.SelectedAccommodationIds != null && vm.SelectedAccommodationIds.Any())
            {
                foreach (var accommodationId in vm.SelectedAccommodationIds.Distinct())
                {
                    promotion.PromotionAccommodations.Add(new PromotionAccommodation
                    {
                        AccommodationId = accommodationId
                    });
                }
            }

            _context.Promotions.Add(promotion);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thêm khuyến mãi thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var promotion = await _context.Promotions
                .Include(p => p.PromotionAccommodations)
                .FirstOrDefaultAsync(p => p.Id == id.Value);

            if (promotion == null) return NotFound();

            var vm = new PromotionVM
            {
                Id = promotion.Id,
                Code = promotion.Code,
                Title = promotion.Title,
                Description = promotion.Description,
                DiscountType = promotion.DiscountType,
                DiscountValue = promotion.DiscountValue,
                MaxDiscountAmount = promotion.MaxDiscountAmount,
                MinBookingAmount = promotion.MinBookingAmount,
                StartDate = promotion.StartDate,
                EndDate = promotion.EndDate,
                UsageLimit = promotion.UsageLimit,
                PerUserLimit = promotion.PerUserLimit,
                BannerImageUrl = promotion.BannerImageUrl,
                Status = promotion.Status,
                SelectedAccommodationIds = promotion.PromotionAccommodations
                    .Select(pa => pa.AccommodationId)
                    .ToList()
            };

            await LoadPromotionFormData(vm);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PromotionVM vm)
        {
            if (id != vm.Id) return NotFound();

            var promotion = await _context.Promotions
                .Include(p => p.PromotionAccommodations)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (promotion == null) return NotFound();

            NormalizePromotionVM(vm);
            await ValidatePromotionVM(vm, id);

            if (!ModelState.IsValid)
            {
                await LoadPromotionFormData(vm);
                return View(vm);
            }

            if (vm.BannerFile != null && vm.BannerFile.Length > 0)
            {
                try
                {
                    var oldBannerUrl = promotion.BannerImageUrl;
                    promotion.BannerImageUrl = await SaveBannerAsync(vm.BannerFile);
                    DeleteLocalFile(oldBannerUrl);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(nameof(vm.BannerFile), ex.Message);
                    await LoadPromotionFormData(vm);
                    return View(vm);
                }
            }
            else
            {
                promotion.BannerImageUrl = vm.BannerImageUrl;
            }

            promotion.Code = vm.Code;
            promotion.Title = vm.Title.Trim();
            promotion.Description = vm.Description;
            promotion.DiscountType = vm.DiscountType;
            promotion.DiscountValue = vm.DiscountValue;
            promotion.MaxDiscountAmount = vm.MaxDiscountAmount;
            promotion.MinBookingAmount = vm.MinBookingAmount;
            promotion.StartDate = vm.StartDate;
            promotion.EndDate = vm.EndDate;
            promotion.UsageLimit = vm.UsageLimit;
            promotion.PerUserLimit = vm.PerUserLimit;
            promotion.Status = vm.Status;
            promotion.UpdatedAt = DateTime.Now;

            _context.PromotionAccommodations.RemoveRange(promotion.PromotionAccommodations);

            if (vm.SelectedAccommodationIds != null && vm.SelectedAccommodationIds.Any())
            {
                foreach (var accommodationId in vm.SelectedAccommodationIds.Distinct())
                {
                    _context.PromotionAccommodations.Add(new PromotionAccommodation
                    {
                        PromotionId = promotion.Id,
                        AccommodationId = accommodationId
                    });
                }
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật khuyến mãi thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var promotion = await _context.Promotions
                .Include(p => p.PromotionAccommodations)
                    .ThenInclude(pa => pa.Accommodation)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id.Value);

            if (promotion == null) return NotFound();

            return View(promotion);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var promotion = await _context.Promotions
                .Include(p => p.PromotionAccommodations)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (promotion == null)
            {
                return RedirectToAction(nameof(Index));
            }

            if (promotion.UsedCount > 0)
            {
                TempData["ErrorMessage"] = "Khuyến mãi đã được sử dụng nên không thể xóa. Bạn có thể tắt khuyến mãi thay vì xóa.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            var oldBannerUrl = promotion.BannerImageUrl;

            _context.Promotions.Remove(promotion);
            await _context.SaveChangesAsync();

            DeleteLocalFile(oldBannerUrl);

            TempData["SuccessMessage"] = "Xóa khuyến mãi thành công.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id, string? returnUrl)
        {
            var promotion = await _context.Promotions.FirstOrDefaultAsync(p => p.Id == id);

            if (promotion == null) return NotFound();

            promotion.Status = promotion.Status == "Active" ? "Inactive" : "Active";
            promotion.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = promotion.Status == "Active"
                ? "Đã bật khuyến mãi."
                : "Đã tắt khuyến mãi.";

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadPromotionFormData(PromotionVM vm)
        {
            vm.AvailableAccommodations = await _context.Accommodations
                .OrderBy(a => a.Name)
                .ToListAsync();

            vm.SelectedAccommodationIds ??= new List<int>();
        }

        private void NormalizePromotionVM(PromotionVM vm)
        {
            vm.Code = (vm.Code ?? string.Empty).Trim().ToUpper();

            if (vm.DiscountType != "Percent" && vm.DiscountType != "Amount")
            {
                vm.DiscountType = "Percent";
            }

            if (vm.Status != "Active" && vm.Status != "Inactive")
            {
                vm.Status = "Active";
            }

            if (vm.DiscountType == "Percent")
            {
                if (vm.DiscountValue > 100)
                {
                    vm.DiscountValue = 100;
                }

                if (vm.DiscountValue < 1)
                {
                    vm.DiscountValue = 1;
                }
            }

            if (vm.MinBookingAmount < 0)
            {
                vm.MinBookingAmount = 0;
            }

            if (vm.MaxDiscountAmount.HasValue && vm.MaxDiscountAmount.Value < 0)
            {
                vm.MaxDiscountAmount = 0;
            }
        }

        private async Task ValidatePromotionVM(PromotionVM vm, int? currentId = null)
        {
            if (vm.EndDate <= vm.StartDate)
            {
                ModelState.AddModelError(nameof(vm.EndDate), "Ngày kết thúc phải lớn hơn ngày bắt đầu.");
            }

/*            if (vm.DiscountType == "Percent" && vm.DiscountValue > 100)
            {
                ModelState.AddModelError(nameof(vm.DiscountValue), "Giảm theo phần trăm không được vượt quá 100%.");
            }*/

            if (vm.MinBookingAmount < 0)
            {
                ModelState.AddModelError(nameof(vm.MinBookingAmount), "Giá trị đơn tối thiểu không được âm.");
            }

            if (vm.MaxDiscountAmount.HasValue && vm.MaxDiscountAmount.Value < 0)
            {
                ModelState.AddModelError(nameof(vm.MaxDiscountAmount), "Giảm tối đa không được âm.");
            }

            if (vm.UsageLimit.HasValue && vm.UsageLimit.Value <= 0)
            {
                ModelState.AddModelError(nameof(vm.UsageLimit), "Giới hạn lượt dùng phải lớn hơn 0.");
            }

            if (vm.PerUserLimit <= 0)
            {
                ModelState.AddModelError(nameof(vm.PerUserLimit), "Giới hạn mỗi khách hàng phải lớn hơn 0.");
            }

            bool codeExists = await _context.Promotions.AnyAsync(p =>
                p.Code == vm.Code &&
                (!currentId.HasValue || p.Id != currentId.Value));

            if (codeExists)
            {
                ModelState.AddModelError(nameof(vm.Code), "Mã khuyến mãi này đã tồn tại.");
            }
        }

        private async Task<string?> SaveBannerAsync(IFormFile? file)
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

            if (file.Length > 3 * 1024 * 1024)
            {
                throw new InvalidOperationException("Dung lượng ảnh không được vượt quá 3MB.");
            }

            var uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "promotions");

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

            return $"/uploads/promotions/{fileName}";
        }

        private void DeleteLocalFile(string? fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl)) return;
            if (!fileUrl.StartsWith("/")) return;

            var relativePath = fileUrl
                .TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            return null;
        }
    }
}