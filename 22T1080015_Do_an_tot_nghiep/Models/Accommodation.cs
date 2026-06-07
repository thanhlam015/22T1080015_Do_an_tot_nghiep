using System;
using System.Collections.Generic;

namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class Accommodation
{
    public int Id { get; set; }

    public int DistrictId { get; set; }

    public string Name { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string Description { get; set; } = null!;

    public double? AverageRating { get; set; }

    public int? StarRating { get; set; }

    public int? PropertyTypeId { get; set; }
    public string? Slug { get; set; }

    public string Status { get; set; } = "Active";

    public bool IsFeatured { get; set; }

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public int ViewCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public string AiIndexStatus { get; set; } = "NotIndexed";

    public DateTime? AiLastIndexedAt { get; set; }

    public string? AiIndexError { get; set; }



    public virtual ICollection<AccommodationAmenity> AccommodationAmenities { get; set; } = new List<AccommodationAmenity>();

    public virtual ICollection<AccommodationImage> AccommodationImages { get; set; } = new List<AccommodationImage>();

    public virtual AccommodationRule? AccommodationRule { get; set; }

    public virtual District District { get; set; } = null!;

    public virtual PropertyType? PropertyType { get; set; }

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();

    public virtual ICollection<SavedAccommodation> SavedAccommodations { get; set; } = new List<SavedAccommodation>();
}
