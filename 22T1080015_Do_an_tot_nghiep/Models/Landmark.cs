using System;
using System.Collections.Generic;

namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class Landmark
{
    public int Id { get; set; }

    public int? DistrictId { get; set; }

    public string Name { get; set; } = null!;

    public string? Type { get; set; }

    public virtual District? District { get; set; }
}
