using _22T1080015_Do_an_tot_nghiep.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _22T1080015_Do_an_tot_nghiep.Models;

namespace _22T1080015_Do_an_tot_nghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class VectorDbController : Controller
    {
        private readonly DoAnTotNghiepContext _context;
        private readonly IRagIndexingService _ragIndexingService;

        public VectorDbController(
            DoAnTotNghiepContext context,
            IRagIndexingService ragIndexingService)
        {
            _context = context;
            _ragIndexingService = ragIndexingService;
        }

        public async Task<IActionResult> Index(string? keyword, string? sourceTable, string? status, int page = 1)
        {
            int pageSize = 10;

            var query = _context.RagDocuments
                .Include(d => d.RagChunks)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(d => d.Title.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(sourceTable))
            {
                query = query.Where(d => d.SourceTable == sourceTable);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(d => d.AiIndexStatus == status);
            }

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var documents = await query
                .OrderByDescending(d => d.IndexedAt)
                .ThenByDescending(d => d.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Keyword = keyword;
            ViewBag.SourceTable = sourceTable;
            ViewBag.Status = status;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            ViewBag.TotalDocuments = await _context.RagDocuments.CountAsync();
            ViewBag.TotalChunks = await _context.RagChunks.CountAsync();
            ViewBag.IndexedAccommodations = await _context.Accommodations.CountAsync(a => a.AiIndexStatus == "Indexed");
            ViewBag.ErrorAccommodations = await _context.Accommodations.CountAsync(a => a.AiIndexStatus == "Error");

            return View(documents);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var document = await _context.RagDocuments
                .Include(d => d.RagChunks.OrderBy(c => c.ChunkIndex))
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id.Value);

            if (document == null) return NotFound();

            return View(document);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncAll()
        {
            try
            {
                int syncedCount = await _ragIndexingService.SyncAllAsync();

                if (syncedCount == 0)
                {
                    TempData["InfoMessage"] = "Không còn dữ liệu để đồng bộ.";
                }
                else
                {
                    TempData["SuccessMessage"] = $"Đồng bộ Vector DB thành công. Đã cập nhật {syncedCount} tài liệu.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi đồng bộ Vector DB: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncAccommodation(int id, string? returnUrl)
        {
            try
            {
                bool changed = await _ragIndexingService.SyncAccommodationAsync(id);

                if (changed)
                {
                    TempData["SuccessMessage"] = "Đồng bộ AI cho nơi lưu trú thành công.";
                }
                else
                {
                    TempData["InfoMessage"] = "Nơi lưu trú này không có dữ liệu mới để đồng bộ.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi đồng bộ: " + ex.Message;
            }

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var document = await _context.RagDocuments.FirstOrDefaultAsync(d => d.Id == id);

            if (document == null) return NotFound();

            document.IsActive = !document.IsActive;
            document.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = document.IsActive
                ? "Đã bật tài liệu trong Vector DB."
                : "Đã tắt tài liệu khỏi Vector DB.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var document = await _context.RagDocuments.FirstOrDefaultAsync(d => d.Id == id);

            if (document == null) return NotFound();

            _context.RagDocuments.Remove(document);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã xóa tài liệu khỏi Vector DB.";
            return RedirectToAction(nameof(Index));
        }
    }
}