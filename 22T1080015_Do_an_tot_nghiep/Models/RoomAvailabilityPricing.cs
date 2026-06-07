using System;
using System.Collections.Generic;

namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class RoomAvailabilityPricing
{
    public int Id { get; set; }

    public int RoomId { get; set; }

    public DateOnly TargetDate { get; set; }

    public decimal PricePerNight { get; set; }

    public int AvailableRooms { get; set; }

    public virtual Room Room { get; set; } = null!;
}
