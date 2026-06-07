namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class RoomImage
{
    public int ImageId { get; set; }

    public int RoomId { get; set; }

    public string ImageUrl { get; set; } = null!;

    public bool IsPrimary { get; set; }

    public int? SortOrder { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Room Room { get; set; } = null!;
}