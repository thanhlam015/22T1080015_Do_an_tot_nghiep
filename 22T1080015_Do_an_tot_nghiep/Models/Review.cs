using System;
using System.Collections.Generic;

namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class Review
{
    public int Id { get; set; }

    public int AccommodationId { get; set; }

    public int UserId { get; set; }

    public int? Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual Accommodation Accommodation { get; set; } = null!;
}
