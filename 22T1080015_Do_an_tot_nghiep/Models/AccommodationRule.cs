using System;
using System.Collections.Generic;

namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class AccommodationRule
{
    public int AccommodationId { get; set; }

    public string? CheckInTime { get; set; }

    public string? CheckOutTime { get; set; }

    public string? PetPolicy { get; set; }

    public string? AgeRestriction { get; set; }

    public string? CancellationPolicy { get; set; }

    public virtual Accommodation Accommodation { get; set; } = null!;
}
