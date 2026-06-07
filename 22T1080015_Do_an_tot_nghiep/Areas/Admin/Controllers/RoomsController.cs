using _22T1080015_Do_an_tot_nghiep.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace _22T1080015_Do_an_tot_nghiep.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class RoomsController : Controller
    {
        private readonly DoAnTotNghiepContext _context;

        public RoomsController(DoAnTotNghiepContext context)
        {
            _context = context;
        }

        // GET: Admin/Rooms
        public async Task<IActionResult> Index(
            int? accommodationId,
            string? keyword,
            string? priceRange,
            int? amenityId,
            int page = 1)
        {
            int pageSize = 10;

            var query = _context.Rooms
                .Include(r => r.Accommodation)
                .Include(r => r.Amenities)
                .AsQueryable();

            if (accommodationId.HasValue && accommodationId.Value > 0)
            {
                query = query.Where(r => r.AccommodationId == accommodationId.Value);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(r =>
                    r.RoomType.Contains(keyword) ||
                    r.Accommodation.Name.Contains(keyword));
            }

            if (!string.IsNullOrWhiteSpace(priceRange))
            {
                switch (priceRange)
                {
                    case "0-1000000":
                        query = query.Where(r => r.PriceNight < 1000000);
                        break;

                    case "1000000-3000000":
                        query = query.Where(r => r.PriceNight >= 1000000 && r.PriceNight <= 3000000);
                        break;

                    case "3000000-5000000":
                        query = query.Where(r => r.PriceNight > 3000000 && r.PriceNight <= 5000000);
                        break;

                    case "5000000":
                        query = query.Where(r => r.PriceNight > 5000000);
                        break;
                }
            }

            if (amenityId.HasValue && amenityId.Value > 0)
            {
                query = query.Where(r => r.Amenities.Any(a => a.Id == amenityId.Value));
            }

            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (totalPages > 0 && page > totalPages) page = totalPages;

            var rooms = await query
                .OrderByDescending(r => r.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            await LoadIndexData(accommodationId, amenityId);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.AccommodationId = accommodationId;
            ViewBag.Keyword = keyword;
            ViewBag.PriceRange = priceRange;
            ViewBag.AmenityId = amenityId;

            return View(rooms);
        }

        // GET: Admin/Rooms/Create
        public async Task<IActionResult> Create()
        {
            var vm = new RoomVM
            {
                Capacity = 2,
                TotalRooms = 1
            };

            await LoadRoomFormData(vm);

            return View(vm);
        }

        // POST: Admin/Rooms/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoomVM vm)
        {
            if (!ModelState.IsValid)
            {
                await LoadRoomFormData(vm);
                return View(vm);
            }

            var room = new Room
            {
                AccommodationId = vm.AccommodationId,
                RoomType = vm.RoomType.Trim(),
                PriceNight = vm.PriceNight,
                Capacity = vm.Capacity,
                TotalRooms = vm.TotalRooms,
                Description = string.IsNullOrWhiteSpace(vm.Description)
                    ? null
                    : vm.Description.Trim(),
                            RoomSize = vm.RoomSize,
                            BedType = string.IsNullOrWhiteSpace(vm.BedType)
                    ? null
                    : vm.BedType.Trim(),

                AdultCapacity = vm.Capacity,
                ChildCapacity = 0,
                Status = "Active",
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                UpdatedAt = null
            };

            if (vm.SelectedAmenityIds != null && vm.SelectedAmenityIds.Any())
            {
                var selectedAmenities = await _context.Amenities
                    .Where(a => vm.SelectedAmenityIds.Contains(a.Id))
                    .ToListAsync();

                foreach (var amenity in selectedAmenities)
                {
                    room.Amenities.Add(amenity);
                }
            }

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thêm phòng mới thành công.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Rooms/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms
                .Include(r => r.Amenities)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null) return NotFound();

            var vm = new RoomVM
            {
                Id = room.Id,
                AccommodationId = room.AccommodationId,
                RoomType = room.RoomType,
                PriceNight = room.PriceNight,
                Capacity = room.Capacity,
                TotalRooms = room.TotalRooms,
                Description = room.Description,
                RoomSize = room.RoomSize,
                BedType = room.BedType,
                SelectedAmenityIds = room.Amenities.Select(a => a.Id).ToList()
            };

            await LoadRoomFormData(vm);

            return View(vm);
        }

        // POST: Admin/Rooms/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RoomVM vm)
        {
            if (id != vm.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadRoomFormData(vm);
                return View(vm);
            }

            var room = await _context.Rooms
                .Include(r => r.Amenities)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null) return NotFound();

            try
            {
                room.AccommodationId = vm.AccommodationId;
                room.RoomType = vm.RoomType.Trim();
                room.PriceNight = vm.PriceNight;
                room.Capacity = vm.Capacity;
                room.TotalRooms = vm.TotalRooms;
                room.Description = vm.Description;
                room.RoomSize = vm.RoomSize;
                room.BedType = vm.BedType;
                room.AdultCapacity = vm.Capacity;
                room.ChildCapacity = 0;
                room.Status = "Active";
                room.IsDeleted = false;
                room.UpdatedAt = DateTime.Now;

                room.Amenities.Clear();

                if (vm.SelectedAmenityIds != null && vm.SelectedAmenityIds.Any())
                {
                    var selectedAmenities = await _context.Amenities
                        .Where(a => vm.SelectedAmenityIds.Contains(a.Id))
                        .ToListAsync();

                    foreach (var amenity in selectedAmenities)
                    {
                        room.Amenities.Add(amenity);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật phòng thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoomExists(vm.Id)) return NotFound();
                throw;
            }
        }

        // GET: Admin/Rooms/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var room = await _context.Rooms
                .Include(r => r.Accommodation)
                .Include(r => r.Amenities)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null) return NotFound();

            ViewBag.BookingCount = await _context.Bookings
                .CountAsync(b => b.RoomId == id.Value);

            return View(room);
        }

        // POST: Admin/Rooms/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms
                .Include(r => r.Amenities)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
            {
                return RedirectToAction(nameof(Index));
            }

            bool hasBooking = await _context.Bookings.AnyAsync(b => b.RoomId == id);

            if (hasBooking)
            {
                TempData["ErrorMessage"] = "Không thể xóa phòng này vì đã có dữ liệu đặt phòng liên quan.";
                return RedirectToAction(nameof(Delete), new { id });
            }

            var availabilityPricings = _context.RoomAvailabilityPricings
                .Where(x => x.RoomId == id);

            _context.RoomAvailabilityPricings.RemoveRange(availabilityPricings);

            room.Amenities.Clear();

            _context.Rooms.Remove(room);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Xóa phòng thành công.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadIndexData(int? selectedAccommodationId = null, int? selectedAmenityId = null)
        {
            var accommodations = await _context.Accommodations
                .OrderBy(a => a.Name)
                .ToListAsync();

            var amenities = await _context.Amenities
                .Where(a => a.Category == "Room")
                .OrderBy(a => a.Name)
                .ToListAsync();

            ViewBag.Accommodations = new SelectList(accommodations, "Id", "Name", selectedAccommodationId);
            ViewBag.Amenities = new SelectList(amenities, "Id", "Name", selectedAmenityId);
        }

        private async Task LoadRoomFormData(RoomVM vm)
        {
            var accommodations = await _context.Accommodations
                .Where(a => a.Status == "Active")
                .OrderBy(a => a.Name)
                .Select(a => new
                {
                    a.Id,
                    a.Name
                })
                .ToListAsync();

            vm.Accommodations = accommodations
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = a.Name,
                    Selected = a.Id == vm.AccommodationId
                })
                .ToList();

            ViewBag.SelectedAccommodationName = accommodations
                .FirstOrDefault(a => a.Id == vm.AccommodationId)
                ?.Name;

            vm.AvailableAmenities = await _context.Amenities
                .Where(a => a.Category == "Room")
                .OrderBy(a => a.Name)
                .ToListAsync();

            vm.SelectedAmenityIds ??= new List<int>();
        }

        private bool RoomExists(int id)
        {
            return _context.Rooms.Any(e => e.Id == id);
        }
    }
}