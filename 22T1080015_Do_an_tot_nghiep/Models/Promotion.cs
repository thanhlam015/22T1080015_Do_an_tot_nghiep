namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class Promotion
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string DiscountType { get; set; } = null!;

    public decimal DiscountValue { get; set; }

    public decimal? MaxDiscountAmount { get; set; }

    public decimal MinBookingAmount { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public int? UsageLimit { get; set; }

    public int UsedCount { get; set; }

    public int PerUserLimit { get; set; }

    public string? BannerImageUrl { get; set; }

    public string Status { get; set; } = "Active";

    public int? CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual User? CreatedByUser { get; set; }

    public virtual ICollection<PromotionAccommodation> PromotionAccommodations { get; set; } = new List<PromotionAccommodation>();
}