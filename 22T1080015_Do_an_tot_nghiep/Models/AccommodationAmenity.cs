using System;
using System.Collections.Generic;

namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class AccommodationAmenity
{
    public int AccommodationId { get; set; }

    public int AmenityId { get; set; }

    public bool IsHighlighted { get; set; }

    public virtual Accommodation Accommodation { get; set; } = null!;

    public virtual Amenity Amenity { get; set; } = null!;
}
