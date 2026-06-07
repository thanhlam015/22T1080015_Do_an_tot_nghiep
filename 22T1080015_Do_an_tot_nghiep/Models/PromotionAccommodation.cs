namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class PromotionAccommodation
{
    public int PromotionId { get; set; }

    public int AccommodationId { get; set; }

    public virtual Promotion Promotion { get; set; } = null!;

    public virtual Accommodation Accommodation { get; set; } = null!;
}