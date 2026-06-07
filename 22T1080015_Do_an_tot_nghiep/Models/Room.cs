using System;
using System.Collections.Generic;

namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class Room
{
    public int Id { get; set; }

    public int AccommodationId { get; set; }

    public string RoomType { get; set; } = null!;

    public decimal PriceNight { get; set; }

    public int Capacity { get; set; }

    public int TotalRooms { get; set; }

    public string? Description { get; set; }

    public double? RoomSize { get; set; }

    public string? BedType { get; set; }
    public int AdultCapacity { get; set; }

    public int ChildCapacity { get; set; }

    public string Status { get; set; } = "Active";

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<RoomImage> RoomImages { get; set; } = new List<RoomImage>();

    public virtual Accommodation Accommodation { get; set; } = null!;

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual ICollection<RoomAvailabilityPricing> RoomAvailabilityPricings { get; set; } = new List<RoomAvailabilityPricing>();

    public virtual ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();
}
