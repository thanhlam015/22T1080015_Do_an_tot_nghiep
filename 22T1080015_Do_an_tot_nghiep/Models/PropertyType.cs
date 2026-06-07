using System;
using System.Collections.Generic;

namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class PropertyType
{
    public int Id { get; set; }

    public string? NamePropertyTypes { get; set; }

    public virtual ICollection<Accommodation> Accommodations { get; set; } = new List<Accommodation>();
}
