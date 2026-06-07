using _22T1080015_Do_an_tot_nghiep.Helpers;
using _22T1080015_Do_an_tot_nghiep.Models;
using _22T1080015_Do_an_tot_nghiep.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Text.Json;


namespace _22T1080015_Do_an_tot_nghiep.Controllers
{
    public partial class UsersController : Controller
    {
        private readonly DoAnTotNghiepContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private const string UserSearchSessionKey = "UserSearchResultsFilter";
        private const string UserLastAreaKeywordKey = "UserLastAreaKeyword";
        private readonly IConfiguration _configuration;
        private readonly IChatbotService _chatbotService;

        public UsersController(
                DoAnTotNghiepContext context,
                IWebHostEnvironment webHostEnvironment,
                IConfiguration configuration,
                IChatbotService chatbotService)
        {
                    _context = context;
                    _webHostEnvironment = webHostEnvironment;
                    _configuration = configuration;
                    _chatbotService = chatbotService;
        }




        [AllowAnonymous]
        public async Task<IActionResult> Index(UserSearchVM search)
        {
            var sessionSearch = GetSearchFromSession();

            // Khi quay về trang chủ không có query, lấy lại thông tin search gần nhất
            if (!Request.QueryString.HasValue && sessionSearch != null)
            {
                search.Keyword = sessionSearch.Keyword;
                search.CheckInDate = sessionSearch.CheckInDate;
                search.CheckOutDate = sessionSearch.CheckOutDate;
                search.RoomCount = sessionSearch.RoomCount;
                search.AdultCount = sessionSearch.AdultCount;
                search.ChildCount = sessionSearch.ChildCount;
            }

            NormalizeUserSearch(search);

            int stayNights = CalculateStayNights(search);

            var savedIds = await GetSavedAccommodationIdsAsync();

            string? areaKeyword = NormalizeAreaKeyword(search.Keyword);

            if (!string.IsNullOrWhiteSpace(areaKeyword))
            {
                HttpContext.Session.SetString("UserLastAreaKeyword", areaKeyword);
            }
            else
            {
                areaKeyword = HttpContext.Session.GetString("UserLastAreaKeyword");
            }

            search.Keyword = areaKeyword;

            var vm = new UserIndexVM
            {
                Search = search,
                CurrentAreaName = areaKeyword,
                Districts = await _context.Districts
                    .OrderBy(d => d.Name)
                    .ToListAsync()
            };


            var baseQuery = _context.Accommodations
                .Include(a => a.District)
                .Include(a => a.PropertyType)
                .Include(a => a.AccommodationImages)
                .Include(a => a.Rooms)
                .Where(a => a.Status == "Active")
                .AsQueryable();

            var areaQuery = ApplyAreaFilter(baseQuery, areaKeyword);

            var areaAccommodations = await areaQuery
                .OrderByDescending(a => a.IsFeatured)
                .ThenByDescending(a => a.ViewCount)
                .Take(20)
                .ToListAsync();

            vm.Accommodations = areaAccommodations
                .Take(8)
                .Select(a =>
                {
                    var card = ToAccommodationCard(a, savedIds);
                    card.StayNights = stayNights;
                    return card;
                })
                .ToList();

            vm.RecentlySavedAccommodations = await GetRecentlySavedAccommodationsAsync(savedIds);

            vm.PopularDistricts = await GetPopularDistrictsAsync();

            vm.PromotionAccommodations = await GetPromotionAccommodationsAsync(areaKeyword, savedIds);

            vm.IdealAccommodations = await GetIdealAccommodationsAsync(areaKeyword, savedIds);

            vm.PropertyTypeSections = await GetPropertyTypeSectionsAsync(areaKeyword);

            return View(vm);
        }

        [AllowAnonymous]
        public async Task<IActionResult> SearchResults(UserSearchVM search)
        {
            bool hasQuery = Request.QueryString.HasValue;
            bool resetFilters = Request.Query.ContainsKey("resetFilters");
            bool applyFilters = Request.Query.ContainsKey("applyFilters");
            string? removeFilter = Request.Query["removeFilter"].FirstOrDefault();

            var sessionSearch = GetSearchFromSession();

            if (!hasQuery && sessionSearch != null)
            {
                search = sessionSearch;
            }
            else if (hasQuery &&
                     sessionSearch != null &&
                     !applyFilters &&
                     !resetFilters &&
                     string.IsNullOrWhiteSpace(removeFilter))
            {
                search = MergeFilterSessionToNewSearch(search, sessionSearch);
            }

            if (!string.IsNullOrWhiteSpace(removeFilter))
            {
                search = sessionSearch ?? search;
                search = RemoveOneFilter(search, removeFilter);
            }

            if (resetFilters)
            {
                search = KeepOnlyAreaSearch(search);
            }

            if (string.IsNullOrWhiteSpace(search.Keyword))
            {
                var lastArea = HttpContext.Session.GetString(UserLastAreaKeywordKey);

                if (!string.IsNullOrWhiteSpace(lastArea))
                {
                    search.Keyword = lastArea;
                }
            }

            if (string.IsNullOrWhiteSpace(search.Keyword))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập khu vực cần đến.";
                return RedirectToAction(nameof(Index));
            }

            if (resetFilters || applyFilters || !string.IsNullOrWhiteSpace(removeFilter))
            {
                search.Page = 1;
            }

            NormalizeUserSearch(search);
            SaveSearchToSession(search);

            int pageSize = 20;

            var savedIds = await GetSavedAccommodationIdsAsync();
            var activePromotions = await GetActivePromotionsAsync();

            var vm = new UserSearchResultsVM
            {
                Search = search,
                PageSize = pageSize,
                CurrentPage = search.Page,

                Districts = await _context.Districts
                    .OrderBy(d => d.Name)
                    .ToListAsync(),

                PropertyTypes = await _context.PropertyTypes
                    .OrderBy(p => p.NamePropertyTypes)
                    .ToListAsync(),

                AccommodationAmenities = await _context.Amenities
                    .Where(a => a.Category == "Hotel" || a.Category == "Activity")
                    .OrderBy(a => a.Name)
                    .ToListAsync(),

                RoomAmenities = await _context.Amenities
                    .Where(a => a.Category == "Room")
                    .OrderBy(a => a.Name)
                    .ToListAsync(),

                BedTypes = await _context.Rooms
                    .Where(r =>
                        !r.IsDeleted &&
                        r.Status == "Active" &&
                        r.BedType != null &&
                        r.BedType != "")
                    .Select(r => r.BedType!)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync()
            };

            int totalGuests = search.AdultCount + search.ChildCount;
            int guestsPerRoom = (int)Math.Ceiling(totalGuests / (double)search.RoomCount);
            int adultsPerRoom = (int)Math.Ceiling(search.AdultCount / (double)search.RoomCount);
            int childrenPerRoom = (int)Math.Ceiling(search.ChildCount / (double)search.RoomCount);

            bool hasDateFilter =
                search.CheckInDate.HasValue &&
                search.CheckOutDate.HasValue &&
                search.CheckOutDate.Value.Date > search.CheckInDate.Value.Date;
            int stayNights = CalculateStayNights(search);

            vm.HasDateFilter = hasDateFilter;

            DateTime? checkIn = hasDateFilter ? search.CheckInDate!.Value.Date : null;
            DateTime? checkOut = hasDateFilter ? search.CheckOutDate!.Value.Date : null;

            var accommodations = await _context.Accommodations
                .Include(a => a.District)
                .Include(a => a.PropertyType)
                .Include(a => a.AccommodationImages)
                .Include(a => a.AccommodationRule)
                .Include(a => a.AccommodationAmenities)
                    .ThenInclude(aa => aa.Amenity)
                .Include(a => a.Rooms)
                    .ThenInclude(r => r.Bookings)
                .Include(a => a.Rooms)
                    .ThenInclude(r => r.Amenities)
                .Where(a => a.Status == "Active")
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(search.Keyword))
            {
                string keyword = search.Keyword.Trim().ToLower();

                accommodations = accommodations
                    .Where(a =>
                        a.Name.ToLower().Contains(keyword) ||
                        a.Address.ToLower().Contains(keyword) ||
                        a.District.Name.ToLower().Contains(keyword))
                    .ToList();
            }

            if (search.DistrictId.HasValue)
            {
                accommodations = accommodations
                    .Where(a => a.DistrictId == search.DistrictId.Value)
                    .ToList();
            }

            if (search.PropertyTypeId.HasValue)
            {
                accommodations = accommodations
                    .Where(a => a.PropertyTypeId == search.PropertyTypeId.Value)
                    .ToList();
            }

            if (search.StarRating.HasValue)
            {
                accommodations = accommodations
                    .Where(a => a.StarRating.HasValue && a.StarRating.Value >= search.StarRating.Value)
                    .ToList();
            }

            if (search.MinRating.HasValue)
            {
                accommodations = accommodations
                    .Where(a =>
                        NormalizeRatingToTen(a.AverageRating).HasValue &&
                        NormalizeRatingToTen(a.AverageRating)!.Value >= search.MinRating.Value)
                    .ToList();
            }

            if (search.OnlyFeatured)
            {
                accommodations = accommodations
                    .Where(a => a.IsFeatured)
                    .ToList();
            }

            if (search.AllowPets)
            {
                accommodations = accommodations
                    .Where(AccommodationAllowsPets)
                    .ToList();
            }

            var result = new List<UserAccommodationCardVM>();

            foreach (var accommodation in accommodations)
            {
                var suitableRooms = accommodation.Rooms
                    .Where(r =>
                        !r.IsDeleted &&
                        r.Status == "Active" &&
                        IsRoomCapacitySuitable(r, guestsPerRoom, adultsPerRoom, childrenPerRoom) &&
                        IsRoomAvailableForSearch(r, search.RoomCount, checkIn, checkOut))
                    .ToList();

                if (!string.IsNullOrWhiteSpace(search.BedType))
                {
                    suitableRooms = suitableRooms
                        .Where(r =>
                            !string.IsNullOrWhiteSpace(r.BedType) &&
                            r.BedType.Contains(search.BedType, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (search.MinRoomSize.HasValue)
                {
                    suitableRooms = suitableRooms
                        .Where(r => r.RoomSize.HasValue && r.RoomSize.Value >= search.MinRoomSize.Value)
                        .ToList();
                }

                if (search.AccommodationAmenityIds != null && search.AccommodationAmenityIds.Any())
                {
                    bool accommodationHasAllAmenities = search.AccommodationAmenityIds
                        .All(amenityId =>
                            accommodation.AccommodationAmenities.Any(aa => aa.AmenityId == amenityId));

                    if (!accommodationHasAllAmenities)
                    {
                        continue;
                    }
                }

                if (search.RoomAmenityIds != null && search.RoomAmenityIds.Any())
                {
                    suitableRooms = suitableRooms
                        .Where(r =>
                            search.RoomAmenityIds.All(amenityId =>
                                r.Amenities.Any(a => a.Id == amenityId)))
                        .ToList();
                }

                if (!suitableRooms.Any())
                {
                    continue;
                }

                decimal minPrice = suitableRooms.Min(r => r.PriceNight);

                if (search.MinPrice.HasValue && minPrice < search.MinPrice.Value)
                {
                    continue;
                }

                if (search.MaxPrice.HasValue && minPrice > search.MaxPrice.Value)
                {
                    continue;
                }

                var promotion = FindPromotionForAccommodation(accommodation, activePromotions);

                if (search.HasPromotion && promotion == null)
                {
                    continue;
                }

                var card = ToAccommodationCard(
                    accommodation,
                    savedIds,
                    promotion?.Code,
                    promotion?.Title);

                card.MinPrice = minPrice;
                card.AvailableRoomTypes = suitableRooms.Count;
                card.StayNights = stayNights;

                result.Add(card);
            }

            result = search.SortBy switch
            {
                "price_asc" => result.OrderBy(x => x.MinPrice).ToList(),
                "price_desc" => result.OrderByDescending(x => x.MinPrice).ToList(),
                "rating_desc" => result.OrderByDescending(x => x.AverageRating ?? 0).ToList(),
                "star_desc" => result.OrderByDescending(x => x.StarRating ?? 0).ToList(),
                "name_asc" => result.OrderBy(x => x.Name).ToList(),
                _ => result
                    .OrderByDescending(x => x.HasPromotion)
                    .ThenByDescending(x => x.AverageRating ?? 0)
                    .ThenBy(x => x.MinPrice)
                    .ToList()
            };

            vm.TotalItems = result.Count;
            vm.TotalPages = (int)Math.Ceiling(vm.TotalItems / (double)pageSize);

            if (search.Page < 1)
            {
                search.Page = 1;
            }

            if (vm.TotalPages > 0 && search.Page > vm.TotalPages)
            {
                search.Page = vm.TotalPages;
            }

            vm.CurrentPage = search.Page;

            vm.Results = result
                .Skip((search.Page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ModelState.Clear();
            return View(vm);
        }

   
    }
}