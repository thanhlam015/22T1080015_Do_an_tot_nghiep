using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class AccommodationImage
{
    [Key] // 1. Thêm dòng này để báo cho EF biết đây là Khóa chính
    public int ImageId { get; set; }

    public int AccommodationId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public bool IsPrimary { get; set; }

    public int? SortOrder { get; set; }

    [ForeignKey("AccommodationId")] // 2. Thêm dòng này để báo đây là Khóa ngoại
    public virtual Accommodation? Accommodation { get; set; }
}
