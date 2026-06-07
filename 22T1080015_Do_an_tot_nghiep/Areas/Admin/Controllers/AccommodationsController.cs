using _22T1080015_Do_an_tot_nghiep.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace _22T1080015_Do_an_tot_nghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AccommodationsController : Controller
    {
        private readonly DoAnTotNghiepContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AccommodationsController(DoAnTotNghiepContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/Accommodations
        public async Task<IActionResult> Index(
    string? keyword,
    int? districtId,
    int? propertyTypeId,
    string? aiStatus,
    int? starRating,
    int page = 1)
        {
            int pageSize = 10;

            var query = _context.Accommodations
                .Include(a => a.PropertyType)
                .Include(a => a.District)
                .Include(a => a.AccommodationImages)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();

                query = query.Where(a =>
                    a.Name.Contains(keyword) ||
                    a.Address.Contains(keyword) ||
                    a.Description.Contains(keyword));
            }

            if (districtId.HasValue && districtId.Value > 0)
            {
                query = query.Where(a => a.DistrictId == districtId.Value);
            }

            if (propertyTypeId.HasValue && propertyTypeId.Value > 0)
            {
                query = query.Where(a => a.PropertyTypeId == propertyTypeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(aiStatus))
            {
                query = query.Where(a => a.AiIndexStatus == aiStatus);
            }

            if (starRating.HasValue && starRating.Value > 0)
            {
                query = query.Where(a => a.StarRating == starRating.Value);
            }

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var accommodations = await query
                .OrderByDescending(a => a.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            ViewBag.Keyword = keyword;
            ViewBag.DistrictId = districtId;
            ViewBag.PropertyTypeId = propertyTypeId;
            ViewBag.AiStatus = aiStatus;
            ViewBag.StarRating = starRating;

            ViewBag.Districts = new SelectList(
                await _context.Districts.OrderBy(d => d.Name).ToListAsync(),
                "Id",
                "Name",
                districtId);

            ViewBag.PropertyTypes = new SelectList(
                await _context.PropertyTypes.OrderBy(p => p.NamePropertyTypes).ToListAsync(),
                "Id",
                "NamePropertyTypes",
                propertyTypeId);

            ViewData["PaginationRouteValues"] = new Dictionary<string, object?>
            {
                ["keyword"] = keyword,
                ["districtId"] = districtId,
                ["propertyTypeId"] = propertyTypeId,
                ["aiStatus"] = aiStatus,
                ["starRating"] = starRating
            };

            return View(accommodations);
        }
        // 2. THÊM MỚI (GET)
        public IActionResult Create()
        {
            // Load danh sách loại chỗ nghỉ vào Dropdown
            var vm = new AccommodationVM
            {
                Districts = new SelectList(_context.Districts, "Id", "Name"),
                PropertyTypes = new SelectList(_context.PropertyTypes, "Id", "NamePropertyTypes"),
                AvailableAmenities = _context.Amenities.Where(a => a.Category == "Hotel").ToList(), // Chỉ lấy tiện ích chung
                // Khởi tạo list rỗng để tránh lỗi Contains(amenity.Id) bị null
                SelectedAmenityIds = new List<int>()

            };
            return View(vm); ;
        }

        // 2. THÊM MỚI (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AccommodationVM vm)
        {
            if (ModelState.IsValid)
            {
                // 1. Lưu Nơi lưu trú
                var accommodation = new Accommodation
                {
                    Name = vm.Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(vm.Description)
                    ? "Chưa cập nhật mô tả."
                    : vm.Description.Trim(),

                                Address = string.IsNullOrWhiteSpace(vm.Address)
                    ? "Chưa cập nhật địa chỉ."
                    : vm.Address.Trim(),

                    DistrictId = vm.DistrictId,
                    PropertyTypeId = vm.PropertyTypeId,
                    StarRating = vm.StarRating,
                    Latitude = vm.Latitude,
                    Longitude = vm.Longitude,

                    Status = "Active",
                    IsFeatured = false,
                    ViewCount = 0,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = null,

                    AiIndexStatus = "NotIndexed",
                    AiLastIndexedAt = null,
                    AiIndexError = null
                };
                _context.Accommodations.Add(accommodation);
                await _context.SaveChangesAsync(); // Lưu để lấy được accommodation.Id

                // 2. Xử lý lưu Tiện ích chung (Accommodation_Amenities)
                if (vm.SelectedAmenityIds != null && vm.SelectedAmenityIds.Any())
                {
                    foreach (var amenityId in vm.SelectedAmenityIds)
                    {
                        _context.AccommodationAmenities.Add(new AccommodationAmenity
                        {
                            AccommodationId = accommodation.Id,
                            AmenityId = amenityId,
                            IsHighlighted = false // Cột này bắt buộc trong SQL của bạn
                        });
                    }
                }

                // 3. Xử lý Upload Hình ảnh
                if (vm.UploadImages != null && vm.UploadImages.Any())
                {
                    string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "image");
                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                    bool isFirstImage = true;
                    foreach (var file in vm.UploadImages)
                    {
                        // Tạo tên file độc nhất để không bị trùng
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                        string filePath = Path.Combine(uploadFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }

                        _context.AccommodationImages.Add(new AccommodationImage
                        {
                            AccommodationId = accommodation.Id,
                            ImageUrl = "/image/" + uniqueFileName,
                            IsPrimary = isFirstImage, // Ảnh đầu tiên được đánh dấu là ảnh bìa
                            SortOrder = 0
                        });
                        isFirstImage = false;
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Nếu có lỗi, nạp lại Dropdown
            vm.Districts = new SelectList(_context.Districts, "Id", "Name");
            vm.PropertyTypes = new SelectList(_context.PropertyTypes, "Id", "NamePropertyTypes");
            vm.AvailableAmenities = _context.Amenities.Where(a => a.Category == "Hotel").ToList();
            return View(vm);
        }

        // 3. CHỈNH SỬA (GET)
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var accommodation = await _context.Accommodations
                .Include(a => a.AccommodationAmenities)
                .Include(a => a.AccommodationImages)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (accommodation == null) return NotFound();

            var vm = new AccommodationVM
            {
                Id = accommodation.Id,
                Name = accommodation.Name,
                Address = accommodation.Address,
                Description = accommodation.Description,
                StarRating = accommodation.StarRating,
                DistrictId = accommodation.DistrictId,
                PropertyTypeId = accommodation.PropertyTypeId,
                Latitude = accommodation.Latitude,
                Longitude = accommodation.Longitude,
                SelectedAmenityIds = accommodation.AccommodationAmenities.Select(a => a.AmenityId).ToList(),

                // Giả sử trong VM bạn có list này để hiện ảnh cũ
                ExistingImages = accommodation.AccommodationImages.ToList(),

                Districts = new SelectList(_context.Districts, "Id", "Name", accommodation.DistrictId),
                PropertyTypes = new SelectList(_context.PropertyTypes, "Id", "NamePropertyTypes", accommodation.PropertyTypeId),
                AvailableAmenities = _context.Amenities.Where(a => a.Category == "Hotel").ToList()
            };

            return View(vm);
        }

        // 3. CHỈNH SỬA (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AccommodationVM vm)
        {
            if (id != vm.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var accommodation = await _context.Accommodations.FindAsync(id);
                    if (accommodation == null) return NotFound();

                    // Cập nhật thông tin cơ bản
                    accommodation.Name = vm.Name;
                    accommodation.Address = vm.Address;
                    accommodation.Description = vm.Description;
                    accommodation.StarRating = vm.StarRating;
                    accommodation.DistrictId = vm.DistrictId;
                    accommodation.PropertyTypeId = vm.PropertyTypeId;
                    accommodation.Latitude = vm.Latitude;
                    accommodation.Longitude = vm.Longitude;
                    accommodation.AiIndexStatus = "NotIndexed";
                    accommodation.AiLastIndexedAt = null;
                    accommodation.AiIndexError = null;

                    _context.Update(accommodation);

                    // Cập nhật Tiện ích (Xóa cũ thêm mới)
                    var existingAmenities = _context.AccommodationAmenities.Where(a => a.AccommodationId == id);
                    _context.AccommodationAmenities.RemoveRange(existingAmenities);

                    if (vm.SelectedAmenityIds != null)
                    {
                        foreach (var amId in vm.SelectedAmenityIds)
                        {
                            _context.AccommodationAmenities.Add(new AccommodationAmenity
                            {
                                AccommodationId = id,
                                AmenityId = amId,
                                IsHighlighted = false
                            });
                        }
                    }

                    // Xử lý upload thêm ảnh mới
                    if (vm.UploadImages != null && vm.UploadImages.Any())
                    {
                        string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "image");
                        foreach (var file in vm.UploadImages)
                        {
                            string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                            string filePath = Path.Combine(uploadFolder, uniqueFileName);
                            using (var fileStream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(fileStream);
                            }
                            _context.AccommodationImages.Add(new AccommodationImage
                            {
                                AccommodationId = id,
                                ImageUrl = "/image/" + uniqueFileName,
                                IsPrimary = false,
                                SortOrder = 0
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException) { if (!AccommodationExists(vm.Id)) return NotFound(); else throw; }
            }
            LoadAccommodationFormData(vm);
            return View(vm);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var image = await _context.AccommodationImages
                .FirstOrDefaultAsync(x => x.ImageId == imageId);

            if (image == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy ảnh cần xóa."
                });
            }

            try
            {
                int accommodationId = image.AccommodationId;

                bool needNewPrimaryImage = image.IsPrimary ||
                    !await _context.AccommodationImages.AnyAsync(x =>
                        x.AccommodationId == accommodationId &&
                        x.ImageId != imageId &&
                        x.IsPrimary);

                // Chỉ xóa file vật lý nếu ảnh nằm trong wwwroot, ví dụ /image/abc.png
                // Nếu ảnh là link ngoài như https://... thì chỉ xóa trong database
                if (!string.IsNullOrWhiteSpace(image.ImageUrl) && image.ImageUrl.StartsWith("/"))
                {
                    var relativePath = image.ImageUrl
                        .TrimStart('/')
                        .Replace('/', Path.DirectorySeparatorChar);

                    var filePath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);

                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                    }
                }

                _context.AccommodationImages.Remove(image);

                // Nếu xóa ảnh chính thì chọn ảnh còn lại làm ảnh chính
                if (needNewPrimaryImage)
                {
                    var nextPrimaryImage = await _context.AccommodationImages
                        .Where(x => x.AccommodationId == accommodationId && x.ImageId != imageId)
                        .OrderBy(x => x.SortOrder ?? int.MaxValue)
                        .ThenBy(x => x.ImageId)
                        .FirstOrDefaultAsync();

                    if (nextPrimaryImage != null)
                    {
                        nextPrimaryImage.IsPrimary = true;
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Đã xóa ảnh thành công."
                });
            }
            catch (Exception ex)
            {
                Response.StatusCode = StatusCodes.Status500InternalServerError;

                return Json(new
                {
                    success = false,
                    message = "Không thể xóa ảnh: " + ex.Message
                });
            }
        }

        // 4. XÓA (Xác nhận và Xóa)
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var item = await _context.Accommodations
                .Include(a => a.PropertyType)
                .Include(a => a.District)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var item = await _context.Accommodations.FindAsync(id);

            if (item != null)
            {
                try
                {
                    // 1. Xóa Hình ảnh
                    var images = _context.AccommodationImages.Where(x => x.AccommodationId == id);
                    _context.AccommodationImages.RemoveRange(images);

                    // 2. Xóa Tiện ích nơi lưu trú
                    var amenities = _context.AccommodationAmenities.Where(x => x.AccommodationId == id);
                    _context.AccommodationAmenities.RemoveRange(amenities);

                    // 3. Xóa Quy định
                    var rules = _context.AccommodationRules.Where(x => x.AccommodationId == id);
                    _context.AccommodationRules.RemoveRange(rules);

                    // 4. Xóa khỏi Danh sách đã lưu của User
                    var saved = _context.SavedAccommodations.Where(x => x.AccommodationId == id);
                    _context.SavedAccommodations.RemoveRange(saved);

                    // 5. Xóa các Phòng thuộc nơi lưu trú này
                    var rooms = _context.Rooms.Where(x => x.AccommodationId == id);
                    _context.Rooms.RemoveRange(rooms);

                    _context.Accommodations.Remove(item);

                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Không thể xóa nơi lưu trú này vì nó đang chứa dữ liệu quan trọng .";
                    return RedirectToAction(nameof(Delete), new { id = id });
                }
            }
           


            return RedirectToAction(nameof(Index));
        }
        private void LoadAccommodationFormData(AccommodationVM vm)
        {
            vm.Districts = new SelectList(_context.Districts, "Id", "Name", vm.DistrictId);

            vm.PropertyTypes = new SelectList(
                _context.PropertyTypes,
                "Id",
                "NamePropertyTypes",
                vm.PropertyTypeId
            );

            vm.AvailableAmenities = _context.Amenities
                .Where(a => a.Category == "Hotel")
                .ToList();

            if (vm.Id > 0)
            {
                vm.ExistingImages = _context.AccommodationImages
                    .Where(x => x.AccommodationId == vm.Id)
                    .OrderBy(x => x.SortOrder ?? int.MaxValue)
                    .ThenBy(x => x.ImageId)
                    .ToList();
            }
        }
        private bool AccommodationExists(int id)
        {
            return _context.Accommodations.Any(e => e.Id == id);
        }
    }
}
