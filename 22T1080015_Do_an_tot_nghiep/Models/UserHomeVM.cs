using System.ComponentModel.DataAnnotations;

namespace _22T1080015_Do_an_tot_nghiep.Models
{
    public class UserSearchVM
    {
        public string? Keyword { get; set; }

        public int? DistrictId { get; set; }

        public int? PropertyTypeId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CheckInDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CheckOutDate { get; set; }

        public int RoomCount { get; set; } = 1;

        public int AdultCount { get; set; } = 2;

        public int ChildCount { get; set; } = 0;

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public string? SortBy { get; set; } = "recommended";

        public int Page { get; set; } = 1;
        public int? StarRating { get; set; }

        public double? MinRating { get; set; }

        public List<int> AmenityIds { get; set; } = new List<int>();

        public string? BedType { get; set; }

        public double? MinRoomSize { get; set; }

        public bool OnlyFeatured { get; set; }

        public bool HasPromotion { get; set; }

        public bool AllowPets { get; set; }

        public string ViewMode { get; set; } = "list";
        public List<int> AccommodationAmenityIds { get; set; } = new List<int>();

        public List<int> RoomAmenityIds { get; set; } = new List<int>();
    }

    public class UserAccommodationCardVM
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string? DistrictName { get; set; }

        public string? PropertyTypeName { get; set; }

        public string? ImageUrl { get; set; }

        public decimal MinPrice { get; set; }

        public int AvailableRoomTypes { get; set; }

        public double? AverageRating { get; set; }

        public bool IsSaved { get; set; }

        public string? PromotionCode { get; set; }

        public string? PromotionTitle { get; set; }
        public int? StarRating { get; set; }

        public bool HasPromotion { get; set; }
        public int StayNights { get; set; } = 0;

        public decimal DisplayPrice
        {
            get
            {
                return StayNights > 0 ? MinPrice * StayNights : MinPrice;
            }
        }

        public string PriceSuffix
        {
            get
            {
                return StayNights > 0 ? $"cho {StayNights} đêm" : "/ đêm";
            }
        }

        public List<string> AmenityNames { get; set; } = new List<string>();
    }

    public class UserDistrictStatVM
    {
        public int DistrictId { get; set; }

        public string DistrictName { get; set; } = string.Empty;

        public int AccommodationCount { get; set; }

        public int BookingCount { get; set; }
    }

    public class UserPropertyTypeSectionVM
    {
        public int PropertyTypeId { get; set; }

        public string PropertyTypeName { get; set; } = string.Empty;

        public int AccommodationCount { get; set; }

        public decimal MinPrice { get; set; }
    }

    public class UserIndexVM
    {
        public UserSearchVM Search { get; set; } = new UserSearchVM();

        public List<District> Districts { get; set; } = new List<District>();

        public List<UserAccommodationCardVM> Accommodations { get; set; } = new List<UserAccommodationCardVM>();

        public List<UserAccommodationCardVM> RecentlySavedAccommodations { get; set; } = new List<UserAccommodationCardVM>();

        public List<UserDistrictStatVM> PopularDistricts { get; set; } = new List<UserDistrictStatVM>();

        public List<UserAccommodationCardVM> PromotionAccommodations { get; set; } = new List<UserAccommodationCardVM>();

        public List<UserAccommodationCardVM> IdealAccommodations { get; set; } = new List<UserAccommodationCardVM>();

        public List<UserPropertyTypeSectionVM> PropertyTypeSections { get; set; } = new List<UserPropertyTypeSectionVM>();

        public string? CurrentAreaName { get; set; }
    }

    public class UserAccommodationDetailVM
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? DistrictName { get; set; }

        public string? PropertyTypeName { get; set; }

        public double? AverageRating { get; set; }

        public int? StarRating { get; set; }

        public decimal MinPrice { get; set; }

        public int StayNights { get; set; }
        public double RatingAverage { get; set; }

        public int TotalReviews { get; set; }

        public Dictionary<int, int> RatingSummary { get; set; } = new Dictionary<int, int>();

        public int ReviewPage { get; set; } = 1;

        public int TotalReviewPages { get; set; }

        public UserSearchVM Search { get; set; } = new UserSearchVM();

        public List<string> Images { get; set; } = new List<string>();

        public List<string> AccommodationAmenities { get; set; } = new List<string>();
        public List<string> HighlightedAmenities { get; set; } = new List<string>();

        public List<string> AllAccommodationAmenities { get; set; } = new List<string>();

        public UserAccommodationRuleVM? Rules { get; set; }

        public List<UserRoomOptionVM> Rooms { get; set; } = new List<UserRoomOptionVM>();

        public List<UserReviewItemVM> Reviews { get; set; } = new List<UserReviewItemVM>();

        public bool CanReview { get; set; }

        public bool HasReviewed { get; set; }

        public bool IsLoggedIn { get; set; }
    }

    public class UserAccommodationRuleVM
    {
        public string? CheckInTime { get; set; }

        public string? CheckOutTime { get; set; }

        public string? PetPolicy { get; set; }

        public string? AgeRestriction { get; set; }

        public string? CancellationPolicy { get; set; }
    }

    public class UserRoomOptionVM
    {
        public int Id { get; set; }

        public string RoomType { get; set; } = string.Empty;

        public decimal PriceNight { get; set; }

        public decimal DisplayPrice { get; set; }

        public string PriceText { get; set; } = string.Empty;

        public int StayNights { get; set; }

        public int Capacity { get; set; }

        public int AdultCapacity { get; set; }

        public int ChildCapacity { get; set; }

        public int TotalRooms { get; set; }

        public int AvailableRooms { get; set; }

        public bool IsAvailable { get; set; }

        public string? Description { get; set; }

        public double? RoomSize { get; set; }

        public string? BedType { get; set; }

        public string? ImageUrl { get; set; }

        public List<string> Amenities { get; set; } = new List<string>();
    }

    //public class UserReviewItemVM
    //{
    //    public int Id { get; set; }

    //    public string UserName { get; set; } = string.Empty;

    //    public string? AvatarUrl { get; set; }

    //    public int Rating { get; set; }

    //    public string? Comment { get; set; }

    //    public DateTime CreatedAt { get; set; }
    //}
}