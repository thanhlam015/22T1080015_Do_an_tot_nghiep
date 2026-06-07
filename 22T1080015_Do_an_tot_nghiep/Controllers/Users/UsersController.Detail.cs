using _22T1080015_Do_an_tot_nghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _22T1080015_Do_an_tot_nghiep.Controllers
{
    public partial class UsersController
    {
        //Details
        [AllowAnonymous]
        public async Task<IActionResult> Details(
    int id,
    DateTime? checkInDate,
    DateTime? checkOutDate,
    int roomCount = 1,
    int adultCount = 2,
    int childCount = 0,
    int reviewPage = 1)
        {
            if (reviewPage < 1)
            {
                reviewPage = 1;
            }

            int reviewPageSize = 5;

            var reviewQuery = _context.Reviews
                .Include(r => r.User)
                .Where(r => r.AccommodationId == id);

            int totalReviews = await reviewQuery.CountAsync();

            int totalReviewPages = (int)Math.Ceiling(totalReviews / (double)reviewPageSize);

            if (totalReviewPages > 0 && reviewPage > totalReviewPages)
            {
                reviewPage = totalReviewPages;
            }

            var ratingSummary = await reviewQuery
                .Where(r => r.Rating.HasValue)
                .GroupBy(r => r.Rating!.Value)
                .Select(g => new
                {
                    Rating = g.Key,
                    Count = g.Count()
                })
                .ToDictionaryAsync(x => x.Rating, x => x.Count);

            double ratingAverage = totalReviews > 0
                ? await reviewQuery
                    .Where(r => r.Rating.HasValue)
                    .AverageAsync(r => (double)r.Rating!.Value)
                : 0;

            var sessionSearch = GetSearchFromSession();

            var search = new UserSearchVM
            {
                CheckInDate = checkInDate ?? sessionSearch?.CheckInDate,
                CheckOutDate = checkOutDate ?? sessionSearch?.CheckOutDate,
                RoomCount = roomCount > 0 ? roomCount : sessionSearch?.RoomCount ?? 1,
                AdultCount = adultCount > 0 ? adultCount : sessionSearch?.AdultCount ?? 2,
                ChildCount = childCount >= 0 ? childCount : sessionSearch?.ChildCount ?? 0
            };

            NormalizeUserSearch(search);
            SaveSearchToSession(search);

            int stayNights = CalculateStayNights(search);

            int totalGuests = search.AdultCount + search.ChildCount;
            int guestsPerRoom = (int)Math.Ceiling(totalGuests / (double)search.RoomCount);
            int adultsPerRoom = (int)Math.Ceiling(search.AdultCount / (double)search.RoomCount);
            int childrenPerRoom = (int)Math.Ceiling(search.ChildCount / (double)search.RoomCount);

            bool hasDateFilter =
                search.CheckInDate.HasValue &&
                search.CheckOutDate.HasValue &&
                search.CheckOutDate.Value.Date > search.CheckInDate.Value.Date;

            DateTime? checkIn = hasDateFilter ? search.CheckInDate!.Value.Date : null;
            DateTime? checkOut = hasDateFilter ? search.CheckOutDate!.Value.Date : null;

            var accommodation = await _context.Accommodations
                .Include(a => a.District)
                .Include(a => a.PropertyType)
                .Include(a => a.AccommodationImages)
                .Include(a => a.AccommodationRule)
                .Include(a => a.AccommodationAmenities)
                    .ThenInclude(aa => aa.Amenity)
                .Include(a => a.Rooms)
                    .ThenInclude(r => r.RoomImages)
                .Include(a => a.Rooms)
                    .ThenInclude(r => r.Amenities)
                .Include(a => a.Rooms)
                    .ThenInclude(r => r.Bookings)
                .Include(a => a.Rooms)
                    .ThenInclude(r => r.RoomAvailabilityPricings)
                .FirstOrDefaultAsync(a => a.Id == id && a.Status == "Active");

            if (accommodation == null)
            {
                return NotFound();
            }

            accommodation.ViewCount++;
            await _context.SaveChangesAsync();

            var suitableRooms = accommodation.Rooms
                .Where(r =>
                    !r.IsDeleted &&
                    r.Status == "Active" &&
                    IsRoomCapacitySuitable(r, guestsPerRoom, adultsPerRoom, childrenPerRoom))
                .OrderBy(r => r.PriceNight)
                .ToList();

            var roomVms = new List<UserRoomOptionVM>();

            foreach (var room in suitableRooms)
            {
                int availableRooms = CalculateAvailableRooms(room, search.RoomCount, checkIn, checkOut);
                decimal displayPrice = CalculateRoomTotalPrice(room, search);

                roomVms.Add(new UserRoomOptionVM
                {
                    Id = room.Id,
                    RoomType = room.RoomType,
                    PriceNight = room.PriceNight,
                    DisplayPrice = displayPrice,
                    StayNights = stayNights,
                    PriceText = stayNights > 0
                        ? $"{displayPrice:N0}đ cho {stayNights} đêm"
                        : "Chọn ngày để xem giá",
                    Capacity = room.Capacity,
                    AdultCapacity = room.AdultCapacity,
                    ChildCapacity = room.ChildCapacity,
                    TotalRooms = room.TotalRooms,
                    AvailableRooms = availableRooms,
                    IsAvailable = availableRooms >= search.RoomCount,
                    Description = room.Description,
                    RoomSize = room.RoomSize,
                    BedType = room.BedType,
                    ImageUrl = room.RoomImages
                        .OrderByDescending(i => i.IsPrimary)
                        .ThenBy(i => i.SortOrder ?? int.MaxValue)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault(),
                    Amenities = room.Amenities
                        .Select(a => a.Name)
                        .Take(8)
                        .ToList()
                });
            }

            var reviews = await reviewQuery
                .OrderByDescending(r => r.CreatedAt)
                .Skip((reviewPage - 1) * reviewPageSize)
                .Take(reviewPageSize)
                .Select(r => new UserReviewItemVM
                {
                    Id = r.Id,
                    UserName = r.User.FullName,
                    AvatarUrl = r.User.AvatarUrl,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            var currentUser = await GetCurrentUserAsync();

            var vm = new UserAccommodationDetailVM
            {
                Id = accommodation.Id,
                Name = accommodation.Name,
                Address = accommodation.Address,
                Description = accommodation.Description,
                DistrictName = accommodation.District?.Name,
                PropertyTypeName = accommodation.PropertyType?.NamePropertyTypes,
                AverageRating = NormalizeRatingToTen(accommodation.AverageRating),
                StarRating = accommodation.StarRating,
                Search = search,
                StayNights = stayNights,
                MinPrice = roomVms.Any() ? roomVms.Min(r => r.PriceNight) : 0,
                IsLoggedIn = currentUser != null,
                CanReview = await CanUserReviewAccommodationAsync(id),
                HasReviewed = await HasUserReviewedAccommodationAsync(id),
                RatingAverage = ratingAverage,
                TotalReviews = totalReviews,
                RatingSummary = ratingSummary,
                ReviewPage = reviewPage,
                TotalReviewPages = totalReviewPages,

                Images = accommodation.AccommodationImages
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.SortOrder ?? int.MaxValue)
                    .Select(i => i.ImageUrl)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .ToList(),

                HighlightedAmenities = accommodation.AccommodationAmenities
                    .Where(x => x.Amenity != null && x.IsHighlighted)
                    .Select(x => x.Amenity.Name)
                    .Take(20)
                    .ToList(),

                AllAccommodationAmenities = accommodation.AccommodationAmenities
                    .Where(x => x.Amenity != null)
                    .Select(x => x.Amenity.Name)
                    .Distinct()
                    .ToList(),

                Rules = accommodation.AccommodationRule == null
                    ? null
                    : new UserAccommodationRuleVM
                    {
                        CheckInTime = accommodation.AccommodationRule.CheckInTime,
                        CheckOutTime = accommodation.AccommodationRule.CheckOutTime,
                        PetPolicy = accommodation.AccommodationRule.PetPolicy,
                        AgeRestriction = accommodation.AccommodationRule.AgeRestriction,
                        CancellationPolicy = accommodation.AccommodationRule.CancellationPolicy
                    },

                Rooms = roomVms,
                Reviews = reviews
            };

            ModelState.Clear();

            return View(vm);
        }
        //IsRoomCapacitySuitable
        private bool IsRoomCapacitySuitable(
                 Room room,
                int guestsPerRoom,
                int adultsPerRoom,
                int childrenPerRoom)
        {
            bool totalCapacityOk = room.Capacity >= guestsPerRoom;

            bool separateCapacityOk =
                room.AdultCapacity >= adultsPerRoom &&
                room.ChildCapacity >= childrenPerRoom;

            return totalCapacityOk || separateCapacityOk;
        }
        //IsRoomAvailableForSearch
        private bool IsRoomAvailableForSearch(
                    Room room,
                    int requestedRoomCount,
                    DateTime? checkIn,
                    DateTime? checkOut)
        {
            if (!checkIn.HasValue || !checkOut.HasValue)
            {
                return room.TotalRooms >= requestedRoomCount;
            }

            int bookedRooms = room.Bookings
                .Where(b =>
                    b.Status != "Cancelled" &&
                    b.CheckInDate.Date < checkOut.Value.Date &&
                    b.CheckOutDate.Date > checkIn.Value.Date)
                .Sum(b => b.NumberOfRooms);

            int availableRooms = room.TotalRooms - bookedRooms;

            return availableRooms >= requestedRoomCount;
        }
        //CalculateStayNights
        private int CalculateStayNights(UserSearchVM search)
        {
            if (!search.CheckInDate.HasValue || !search.CheckOutDate.HasValue)
            {
                return 0;
            }

            var checkIn = search.CheckInDate.Value.Date;
            var checkOut = search.CheckOutDate.Value.Date;

            if (checkOut <= checkIn)
            {
                return 0;
            }

            return (checkOut - checkIn).Days;
        }
        //CalculateAvailableRooms
        private int CalculateAvailableRooms(
                    Room room,
                    int requestedRoomCount,
                    DateTime? checkIn,
                    DateTime? checkOut)
        {
            if (!checkIn.HasValue || !checkOut.HasValue || checkOut.Value.Date <= checkIn.Value.Date)
            {
                return room.TotalRooms;
            }

            int bookedRooms = room.Bookings
                .Where(b =>
                    b.Status != "Cancelled" &&
                    b.CheckInDate.Date < checkOut.Value.Date &&
                    b.CheckOutDate.Date > checkIn.Value.Date)
                .Sum(b => b.NumberOfRooms);

            int availableByBooking = room.TotalRooms - bookedRooms;

            if (room.RoomAvailabilityPricings == null || !room.RoomAvailabilityPricings.Any())
            {
                return availableByBooking;
            }

            var checkInDate = DateOnly.FromDateTime(checkIn.Value.Date);
            var checkOutDate = DateOnly.FromDateTime(checkOut.Value.Date);

            var dayAvailability = room.RoomAvailabilityPricings
                .Where(p => p.TargetDate >= checkInDate && p.TargetDate < checkOutDate)
                .Select(p => p.AvailableRooms)
                .ToList();

            if (!dayAvailability.Any())
            {
                return availableByBooking;
            }

            int availableByCalendar = dayAvailability.Min();

            return Math.Min(availableByBooking, availableByCalendar);
        }
        //CalculateRoomTotalPrice
        private decimal CalculateRoomTotalPrice(Room room, UserSearchVM search)
        {
            int stayNights = CalculateStayNights(search);

            if (stayNights <= 0)
            {
                return room.PriceNight;
            }

            decimal total = 0;

            var checkIn = search.CheckInDate!.Value.Date;

            for (int i = 0; i < stayNights; i++)
            {
                var targetDate = DateOnly.FromDateTime(checkIn.AddDays(i));

                var customPrice = room.RoomAvailabilityPricings?
                    .FirstOrDefault(p => p.TargetDate == targetDate)?
                    .PricePerNight;

                total += customPrice ?? room.PriceNight;
            }

            return total * search.RoomCount;
        }
        //CanUserReviewAccommodationAsync
        private async Task<bool> CanUserReviewAccommodationAsync(int accommodationId)
        {
            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                return false;
            }

            bool hasCompletedBooking = await _context.Bookings
                .Include(b => b.Room)
                .AnyAsync(b =>
                    b.UserId == user.UserId &&
                    b.Room.AccommodationId == accommodationId &&
                    b.Status != "Cancelled" &&
                    b.CheckOutDate <= DateTime.Now);

            if (!hasCompletedBooking)
            {
                return false;
            }

            bool hasReviewed = await _context.Reviews
                .AnyAsync(r =>
                    r.UserId == user.UserId &&
                    r.AccommodationId == accommodationId);

            return !hasReviewed;
        }
        //HasUserReviewedAccommodationAsync
        private async Task<bool> HasUserReviewedAccommodationAsync(int accommodationId)
        {
            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                return false;
            }

            return await _context.Reviews
                .AnyAsync(r =>
                    r.UserId == user.UserId &&
                    r.AccommodationId == accommodationId);
        }
    }
}