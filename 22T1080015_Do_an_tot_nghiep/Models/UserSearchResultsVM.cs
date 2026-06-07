namespace _22T1080015_Do_an_tot_nghiep.Models
{
    public class UserSearchResultsVM
    {
        public UserSearchVM Search { get; set; } = new UserSearchVM();

        public List<District> Districts { get; set; } = new List<District>();

        public List<PropertyType> PropertyTypes { get; set; } = new List<PropertyType>();

        public List<Amenity> Amenities { get; set; } = new List<Amenity>();

        public List<string> BedTypes { get; set; } = new List<string>();

        public List<UserAccommodationCardVM> Results { get; set; } = new List<UserAccommodationCardVM>();
        public List<Amenity> AccommodationAmenities { get; set; } = new List<Amenity>();

        public List<Amenity> RoomAmenities { get; set; } = new List<Amenity>();

        public int TotalItems { get; set; }

        public int CurrentPage { get; set; }

        public int TotalPages { get; set; }

        public int PageSize { get; set; } = 8;

        public bool HasDateFilter { get; set; }
    }
}