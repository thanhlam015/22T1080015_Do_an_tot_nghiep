using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace _22T1080015_Do_an_tot_nghiep.Models
{
    public class RoomVM
    {
        public int Id { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn nơi lưu trú")]
        [Display(Name = "Nơi lưu trú")]
        public int AccommodationId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên loại phòng")]
        [StringLength(50, ErrorMessage = "Tên loại phòng không được vượt quá 50 ký tự")]
        [Display(Name = "Tên loại phòng")]
        public string RoomType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập giá mỗi đêm")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá mỗi đêm không được âm")]
        [Display(Name = "Giá mỗi đêm")]
        public decimal PriceNight { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập sức chứa")]
        [Range(1, int.MaxValue, ErrorMessage = "Sức chứa phải lớn hơn 0")]
        [Display(Name = "Sức chứa")]
        public int Capacity { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tổng số phòng")]
        [Range(1, int.MaxValue, ErrorMessage = "Tổng số phòng phải lớn hơn 0")]
        [Display(Name = "Tổng số phòng")]
        public int TotalRooms { get; set; }

        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Diện tích phòng không được âm")]
        [Display(Name = "Diện tích phòng")]
        public double? RoomSize { get; set; }

        [StringLength(100, ErrorMessage = "Loại giường không được vượt quá 100 ký tự")]
        [Display(Name = "Loại giường")]
        public string? BedType { get; set; }

        public List<int> SelectedAmenityIds { get; set; } = new List<int>();

        public IEnumerable<SelectListItem>? Accommodations { get; set; }

        public List<Amenity>? AvailableAmenities { get; set; }
    }
}