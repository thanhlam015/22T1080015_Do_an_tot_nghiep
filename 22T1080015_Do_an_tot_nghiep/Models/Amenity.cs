using System;
using System.Collections.Generic;

namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class Amenity
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Icon { get; set; } = null!;

    public string? Category { get; set; }

    public virtual ICollection<AccommodationAmenity> AccommodationAmenities { get; set; } = new List<AccommodationAmenity>();

    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
