using System;
using System.Collections.Generic;

namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class District
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<Accommodation> Accommodations { get; set; } = new List<Accommodation>();

    public virtual ICollection<Landmark> Landmarks { get; set; } = new List<Landmark>();
}
