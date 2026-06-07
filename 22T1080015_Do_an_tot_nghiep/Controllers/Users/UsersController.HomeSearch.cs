using _22T1080015_Do_an_tot_nghiep.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace _22T1080015_Do_an_tot_nghiep.Controllers
{
    public partial class UsersController
    {
        //NormalizeUserSearch
        private void NormalizeUserSearch(UserSearchVM search)
        {
            var today = DateTime.Today;

            if (search.CheckInDate.HasValue &&
                search.CheckInDate.Value.Date < today)
            {
                search.CheckInDate = today;
            }

            if (search.CheckOutDate.HasValue &&
                search.CheckOutDate.Value.Date <= today)
            {
                search.CheckOutDate = today.AddDays(1);
            }

            if (search.CheckInDate.HasValue &&
                search.CheckOutDate.HasValue &&
                search.CheckOutDate.Value.Date <= search.CheckInDate.Value.Date)
            {
                search.CheckOutDate = search.CheckInDate.Value.Date.AddDays(1);
            }

            if (search.RoomCount <= 0)
            {
                search.RoomCount = 1;
            }

            if (search.AdultCount <= 0)
            {
                search.AdultCount = 1;
            }

            if (search.ChildCount < 0)
            {
                search.ChildCount = 0;
            }

            if (search.RoomCount > 10)
            {
                search.RoomCount = 10;
            }

            if (search.AdultCount > 30)
            {
                search.AdultCount = 30;
            }

            if (search.ChildCount > 20)
            {
                search.ChildCount = 20;
            }

            if (search.Page <= 0)
            {
                search.Page = 1;
            }

            if (search.MinPrice.HasValue && search.MinPrice.Value <= 0)
            {
                search.MinPrice = null;
            }

            if (search.MaxPrice.HasValue && search.MaxPrice.Value >= 10000000)
            {
                search.MaxPrice = null;
            }

            if (search.MinPrice.HasValue &&
                search.MaxPrice.HasValue &&
                search.MaxPrice.Value < search.MinPrice.Value)
            {
                search.MaxPrice = search.MinPrice;
            }

            var allowedSorts = new[]
            {
                "recommended",
                "price_asc",
                "price_desc",
                "rating_desc",
                "star_desc",
                "name_asc"
            };

            if (string.IsNullOrWhiteSpace(search.SortBy) ||
                !allowedSorts.Contains(search.SortBy))
            {
                search.SortBy = "recommended";
            }
            var allowedViewModes = new[]
                {
                    "list",
                    "grid"
                };

            if (string.IsNullOrWhiteSpace(search.ViewMode) ||
                !allowedViewModes.Contains(search.ViewMode))
            {
                search.ViewMode = "list";
            }

            if (search.StarRating.HasValue)
            {
                if (search.StarRating.Value < 1)
                {
                    search.StarRating = null;
                }
                else if (search.StarRating.Value > 5)
                {
                    search.StarRating = 5;
                }
            }

            if (search.MinRating.HasValue)
            {
                if (search.MinRating.Value < 0)
                {
                    search.MinRating = null;
                }
                else if (search.MinRating.Value > 10)
                {
                    search.MinRating = 10;
                }
            }

            if (search.MinRoomSize.HasValue && search.MinRoomSize.Value < 0)
            {
                search.MinRoomSize = null;
            }
        }
        //NormalizeAreaKeyword
        private string? NormalizeAreaKeyword(string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return null;
            }

            return keyword.Trim();
        }
        //ApplyAreaFilter
        private IQueryable<Accommodation> ApplyAreaFilter(
                IQueryable<Accommodation> query,
                string? areaKeyword)
        {
            if (string.IsNullOrWhiteSpace(areaKeyword))
            {
                return query;
            }

            areaKeyword = areaKeyword.Trim();

            return query.Where(a =>
                a.District.Name.Contains(areaKeyword) ||
                a.Address.Contains(areaKeyword));
        }
        //GetSavedAccommodationIdsAsync
        private async Task<HashSet<int>> GetSavedAccommodationIdsAsync()
        {
            var user = await GetCurrentUserAsync();

            if (user == null)
            {
                return new HashSet<int>();
            }

            var ids = await _context.SavedAccommodations
                .Where(s => s.UserId == user.UserId)
                .Select(s => s.AccommodationId)
                .ToListAsync();

            return ids.ToHashSet();
        }
        //ToAccommodationCard
        private UserAccommodationCardVM ToAccommodationCard(
    Accommodation accommodation,
    HashSet<int> savedIds,
    string? promotionCode = null,
    string? promotionTitle = null)
        {
            var activeRooms = accommodation.Rooms
                .Where(r => !r.IsDeleted && r.Status == "Active")
                .ToList();

            var amenityNames = accommodation.AccommodationAmenities
                .Where(x => x.Amenity != null)
                .Select(x => x.Amenity.Name)
                .Union(
                    activeRooms
                        .SelectMany(r => r.Amenities)
                        .Select(a => a.Name)
                )
                .Distinct()
                .Take(5)
                .ToList();

            return new UserAccommodationCardVM
            {
                Id = accommodation.Id,
                Name = accommodation.Name,
                Address = accommodation.Address,
                DistrictName = accommodation.District?.Name,
                PropertyTypeName = accommodation.PropertyType?.NamePropertyTypes,
                AverageRating = NormalizeRatingToTen(accommodation.AverageRating),
                StarRating = accommodation.StarRating,
                IsSaved = savedIds.Contains(accommodation.Id),
                HasPromotion = !string.IsNullOrWhiteSpace(promotionCode),
                PromotionCode = promotionCode,
                PromotionTitle = promotionTitle,
                AmenityNames = amenityNames,

                ImageUrl = accommodation.AccommodationImages
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.SortOrder ?? int.MaxValue)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault(),

                MinPrice = activeRooms.Any()
                    ? activeRooms.Min(r => r.PriceNight)
                    : 0,

                AvailableRoomTypes = activeRooms.Count
            };
        }
        //GetRecentlySavedAccommodationsAsync
        private async Task<List<UserAccommodationCardVM>> GetRecentlySavedAccommodationsAsync(HashSet<int> savedIds)
        {
            var user = await GetCurrentUserAsync();

            if (user == null || !savedIds.Any())
            {
                return new List<UserAccommodationCardVM>();
            }

            var savedAccommodations = await _context.SavedAccommodations
                .Include(s => s.Accommodation)
                    .ThenInclude(a => a.District)
                .Include(s => s.Accommodation)
                    .ThenInclude(a => a.PropertyType)
                .Include(s => s.Accommodation)
                    .ThenInclude(a => a.AccommodationImages)
                .Include(s => s.Accommodation)
                    .ThenInclude(a => a.Rooms)
                .Where(s =>
                    s.UserId == user.UserId &&
                    s.Accommodation.Status == "Active")
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .Select(s => s.Accommodation)
                .ToListAsync();

            return savedAccommodations
                .Select(a => ToAccommodationCard(a, savedIds))
                .ToList();
        }
        //GetPopularDistrictsAsync
        private async Task<List<UserDistrictStatVM>> GetPopularDistrictsAsync()
        {
            var bookingStats = await _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r.Accommodation)
                        .ThenInclude(a => a.District)
                .Where(b => b.Status != "Cancelled")
                .GroupBy(b => new
                {
                    b.Room.Accommodation.DistrictId,
                    b.Room.Accommodation.District.Name
                })
                .Select(g => new UserDistrictStatVM
                {
                    DistrictId = g.Key.DistrictId,
                    DistrictName = g.Key.Name,
                    BookingCount = g.Count(),
                    AccommodationCount = g
                        .Select(x => x.Room.AccommodationId)
                        .Distinct()
                        .Count()
                })
                .OrderByDescending(x => x.BookingCount)
                .ThenByDescending(x => x.AccommodationCount)
                .Take(5)
                .ToListAsync();

            if (bookingStats.Any())
            {
                return bookingStats;
            }

            return await _context.Accommodations
                .Include(a => a.District)
                .Where(a => a.Status == "Active")
                .GroupBy(a => new
                {
                    a.DistrictId,
                    a.District.Name
                })
                .Select(g => new UserDistrictStatVM
                {
                    DistrictId = g.Key.DistrictId,
                    DistrictName = g.Key.Name,
                    AccommodationCount = g.Count(),
                    BookingCount = 0
                })
                .OrderByDescending(x => x.AccommodationCount)
                .Take(5)
                .ToListAsync();
        }
        //GetPromotionAccommodationsAsync
        private async Task<List<UserAccommodationCardVM>> GetPromotionAccommodationsAsync(
    string? areaKeyword,
    HashSet<int> savedIds)
        {
            var now = DateTime.Now;

            var promotions = await _context.Promotions
                .Include(p => p.PromotionAccommodations)
                .Where(p =>
                    p.Status == "Active" &&
                    p.StartDate <= now &&
                    p.EndDate >= now &&
                    (!p.UsageLimit.HasValue || p.UsedCount < p.UsageLimit.Value))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            if (!promotions.Any())
            {
                return new List<UserAccommodationCardVM>();
            }

            var query = _context.Accommodations
                .Include(a => a.District)
                .Include(a => a.PropertyType)
                .Include(a => a.AccommodationImages)
                .Include(a => a.Rooms)
                .Where(a => a.Status == "Active")
                .AsQueryable();

            query = ApplyAreaFilter(query, areaKeyword);

            var accommodations = await query
                .OrderByDescending(a => a.IsFeatured)
                .ThenByDescending(a => a.AverageRating)
                .Take(30)
                .ToListAsync();

            var result = new List<UserAccommodationCardVM>();

            foreach (var accommodation in accommodations)
            {
                var promotion = promotions.FirstOrDefault(p =>
                    !p.PromotionAccommodations.Any() ||
                    p.PromotionAccommodations.Any(pa => pa.AccommodationId == accommodation.Id));

                if (promotion == null)
                {
                    continue;
                }

                result.Add(ToAccommodationCard(
                    accommodation,
                    savedIds,
                    promotion.Code,
                    promotion.Title));

                if (result.Count >= 8)
                {
                    break;
                }
            }

            return result;
        }
        //GetIdealAccommodationsAsync
        private async Task<List<UserAccommodationCardVM>> GetIdealAccommodationsAsync(
                    string? areaKeyword,
                    HashSet<int> savedIds)
        {
            var query = _context.Accommodations
                .Include(a => a.District)
                .Include(a => a.PropertyType)
                .Include(a => a.AccommodationImages)
                .Include(a => a.Rooms)
                .Where(a =>
                    a.Status == "Active" &&
                    a.AverageRating != null)
                .AsQueryable();

            query = ApplyAreaFilter(query, areaKeyword);

            var accommodations = await query
                .OrderByDescending(a => a.AverageRating)
                .ThenByDescending(a => a.ViewCount)
                .Take(20)
                .ToListAsync();

            return accommodations
                .OrderBy(_ => Guid.NewGuid())
                .Take(5)
                .Select(a => ToAccommodationCard(a, savedIds))
                .ToList();
        }
        //GetPropertyTypeSectionsAsync
        private async Task<List<UserPropertyTypeSectionVM>> GetPropertyTypeSectionsAsync(string? areaKeyword)
        {
            var query = _context.Accommodations
                .Include(a => a.District)
                .Include(a => a.PropertyType)
                .Include(a => a.Rooms)
                .Where(a =>
                    a.Status == "Active" &&
                    a.PropertyTypeId != null)
                .AsQueryable();

            query = ApplyAreaFilter(query, areaKeyword);

            var accommodations = await query.ToListAsync();

            return accommodations
                .Where(a => a.PropertyTypeId != null && a.PropertyType != null)
                .GroupBy(a => new
                {
                    PropertyTypeId = a.PropertyTypeId!.Value,
                    PropertyTypeName = a.PropertyType!.NamePropertyTypes
                })
                .Select(g => new UserPropertyTypeSectionVM
                {
                    PropertyTypeId = g.Key.PropertyTypeId,
                    PropertyTypeName = g.Key.PropertyTypeName,
                    AccommodationCount = g.Count(),
                    MinPrice = g
                        .SelectMany(a => a.Rooms)
                        .Where(r => !r.IsDeleted && r.Status == "Active")
                        .Select(r => (decimal?)r.PriceNight)
                        .Min() ?? 0
                })
                .OrderByDescending(x => x.AccommodationCount)
                .Take(6)
                .ToList();
        }
        //GetActivePromotionsAsync
        private async Task<List<Promotion>> GetActivePromotionsAsync()
        {
            var now = DateTime.Now;

            return await _context.Promotions
                .Include(p => p.PromotionAccommodations)
                .Where(p =>
                    p.Status == "Active" &&
                    p.StartDate <= now &&
                    p.EndDate >= now &&
                    (!p.UsageLimit.HasValue || p.UsedCount < p.UsageLimit.Value))
                .ToListAsync();
        }
        //FindPromotionForAccommodation
        private Promotion? FindPromotionForAccommodation(
                Accommodation accommodation,
                List<Promotion> activePromotions)
        {
            return activePromotions.FirstOrDefault(p =>
                !p.PromotionAccommodations.Any() ||
                p.PromotionAccommodations.Any(pa => pa.AccommodationId == accommodation.Id));
        }
        //AccommodationAllowsPets
        private bool AccommodationAllowsPets(Accommodation accommodation)
        {
            var petPolicy = accommodation.AccommodationRule?.PetPolicy;

            if (string.IsNullOrWhiteSpace(petPolicy))
            {
                return false;
            }

            petPolicy = petPolicy.ToLower();

            return petPolicy.Contains("cho phép") ||
                   petPolicy.Contains("được phép") ||
                   petPolicy.Contains("allow") ||
                   petPolicy.Contains("yes");
        }
        //GetSearchFromSession
        private UserSearchVM? GetSearchFromSession()
        {
            var json = HttpContext.Session.GetString(UserSearchSessionKey);

            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<UserSearchVM>(json);
            }
            catch
            {
                return null;
            }
        }
        //SaveSearchToSession
        private void SaveSearchToSession(UserSearchVM search)
        {
            var json = JsonSerializer.Serialize(search);
            HttpContext.Session.SetString(UserSearchSessionKey, json);

            if (!string.IsNullOrWhiteSpace(search.Keyword))
            {
                HttpContext.Session.SetString(UserLastAreaKeywordKey, search.Keyword.Trim());
            }
        }
        //KeepOnlyAreaSearch
        private UserSearchVM KeepOnlyAreaSearch(UserSearchVM search)
        {
            return new UserSearchVM
            {
                Keyword = search.Keyword,
                RoomCount = 1,
                AdultCount = 2,
                ChildCount = 0,
                SortBy = "recommended",
                ViewMode = search.ViewMode == "grid" ? "grid" : "list",
                Page = 1
            };
        }
        //RemoveOneFilter
        private UserSearchVM RemoveOneFilter(UserSearchVM search, string filterName)
        {
            switch (filterName)
            {
                case "PropertyType":
                    search.PropertyTypeId = null;
                    break;

                case "Price":
                    search.MinPrice = null;
                    search.MaxPrice = null;
                    break;

                case "StarRating":
                    search.StarRating = null;
                    break;

                case "MinRating":
                    search.MinRating = null;
                    break;

                case "BedType":
                    search.BedType = null;
                    break;

                case "MinRoomSize":
                    search.MinRoomSize = null;
                    break;

                case "Amenities":
                    search.AmenityIds = new List<int>();
                    break;

                case "OnlyFeatured":
                    search.OnlyFeatured = false;
                    break;

                case "HasPromotion":
                    search.HasPromotion = false;
                    break;

                case "AllowPets":
                    search.AllowPets = false;
                    break;

                case "Dates":
                    search.CheckInDate = null;
                    search.CheckOutDate = null;
                    break;
                case "AccommodationAmenities":
                    search.AccommodationAmenityIds = new List<int>();
                    break;

                case "RoomAmenities":
                    search.RoomAmenityIds = new List<int>();
                    break;
            }

            search.Page = 1;

            return search;
        }
        //MergeSearchWithSession
        private UserSearchVM MergeSearchWithSession(UserSearchVM submitted, UserSearchVM session)
        {
            submitted.PropertyTypeId = Request.Query.ContainsKey("PropertyTypeId")
                ? submitted.PropertyTypeId
                : session.PropertyTypeId;

            submitted.MinPrice = Request.Query.ContainsKey("MinPrice")
                ? submitted.MinPrice
                : session.MinPrice;

            submitted.MaxPrice = Request.Query.ContainsKey("MaxPrice")
                ? submitted.MaxPrice
                : session.MaxPrice;

            submitted.StarRating = Request.Query.ContainsKey("StarRating")
                ? submitted.StarRating
                : session.StarRating;

            submitted.MinRating = Request.Query.ContainsKey("MinRating")
                ? submitted.MinRating
                : session.MinRating;

            submitted.BedType = Request.Query.ContainsKey("BedType")
                ? submitted.BedType
                : session.BedType;

            submitted.MinRoomSize = Request.Query.ContainsKey("MinRoomSize")
                ? submitted.MinRoomSize
                : session.MinRoomSize;

            submitted.AmenityIds = Request.Query.ContainsKey("AmenityIds")
                ? submitted.AmenityIds
                : session.AmenityIds;

            submitted.OnlyFeatured = Request.Query.ContainsKey("OnlyFeatured")
                ? submitted.OnlyFeatured
                : session.OnlyFeatured;

            submitted.HasPromotion = Request.Query.ContainsKey("HasPromotion")
                ? submitted.HasPromotion
                : session.HasPromotion;

            submitted.AllowPets = Request.Query.ContainsKey("AllowPets")
                ? submitted.AllowPets
                : session.AllowPets;

            submitted.SortBy = Request.Query.ContainsKey("SortBy")
                ? submitted.SortBy
                : session.SortBy;

            submitted.ViewMode = Request.Query.ContainsKey("ViewMode")
                ? submitted.ViewMode
                : session.ViewMode;

            return submitted;
        }
        //MergeFilterSessionToNewSearch
        private UserSearchVM MergeFilterSessionToNewSearch(UserSearchVM submitted, UserSearchVM session)
        {
            submitted.PropertyTypeId = session.PropertyTypeId;
            submitted.MinPrice = session.MinPrice;
            submitted.MaxPrice = session.MaxPrice;
            submitted.StarRating = session.StarRating;
            submitted.MinRating = session.MinRating;
            submitted.BedType = session.BedType;
            submitted.MinRoomSize = session.MinRoomSize;

            submitted.AccommodationAmenityIds = session.AccommodationAmenityIds;
            submitted.RoomAmenityIds = session.RoomAmenityIds;

            submitted.OnlyFeatured = session.OnlyFeatured;
            submitted.HasPromotion = session.HasPromotion;
            submitted.AllowPets = session.AllowPets;
            submitted.SortBy = session.SortBy;
            submitted.ViewMode = session.ViewMode;

            submitted.Page = 1;

            return submitted;
        }
        //NormalizeRatingToTen
        private double? NormalizeRatingToTen(double? rating)
        {
            if (!rating.HasValue)
            {
                return null;
            }

            // Nếu dữ liệu cũ đang là 4.8/5 thì đổi thành 9.6/10.
            if (rating.Value <= 5)
            {
                return Math.Round(rating.Value * 2, 1);
            }

            return Math.Round(rating.Value, 1);
        }
    }
}