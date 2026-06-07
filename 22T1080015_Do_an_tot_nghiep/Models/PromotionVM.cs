using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace _22T1080015_Do_an_tot_nghiep.Models
{
    public class PromotionVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã khuyến mãi")]
        [StringLength(50, ErrorMessage = "Mã khuyến mãi không được vượt quá 50 ký tự")]
        [Display(Name = "Mã khuyến mãi")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên khuyến mãi")]
        [StringLength(150, ErrorMessage = "Tên khuyến mãi không được vượt quá 150 ký tự")]
        [Display(Name = "Tên khuyến mãi")]
        public string Title { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại giảm giá")]
        [Display(Name = "Loại giảm giá")]
        public string DiscountType { get; set; } = "Percent";

        [Required(ErrorMessage = "Vui lòng nhập giá trị giảm")]
        [Range(1, double.MaxValue, ErrorMessage = "Giá trị giảm phải lớn hơn 0")]
        [Display(Name = "Giá trị giảm")]
        public decimal DiscountValue { get; set; }

        [Display(Name = "Giảm tối đa")]
        public decimal? MaxDiscountAmount { get; set; }

        [Display(Name = "Giá trị đơn tối thiểu")]
        public decimal MinBookingAmount { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
        [Display(Name = "Ngày bắt đầu")]
        public DateTime StartDate { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
        [Display(Name = "Ngày kết thúc")]
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(7);

        [Display(Name = "Giới hạn số lượt dùng")]
        public int? UsageLimit { get; set; }

        [Display(Name = "Giới hạn mỗi khách hàng")]
        public int PerUserLimit { get; set; } = 1;

        [Display(Name = "Ảnh banner")]
        public string? BannerImageUrl { get; set; }

        [Display(Name = "Tải ảnh banner")]
        public IFormFile? BannerFile { get; set; }

        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "Active";

        public List<int> SelectedAccommodationIds { get; set; } = new List<int>();

        public List<Accommodation> AvailableAccommodations { get; set; } = new List<Accommodation>();
    }
}