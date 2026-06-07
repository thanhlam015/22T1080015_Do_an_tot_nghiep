using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace _22T1080015_Do_an_tot_nghiep.Models
{
    public class AccommodationVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên nơi lưu trú")]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Address { get; set; }
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn Tỉnh/Thành phố")]
        public int DistrictId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại hình")]
        public int? PropertyTypeId { get; set; }
        public int? StarRating { get; set; }


        // 1. Nhận danh sách File ảnh upload lên
        public List<IFormFile>? UploadImages { get; set; }

        // 2. Nhận danh sách ID của các Tiện ích được check
        public List<int> SelectedAmenityIds { get; set; } = new List<int>();

        // 3. Dữ liệu mồi để hiển thị ra View (Dropdown, Checkbox)
        public IEnumerable<SelectListItem>? Districts { get; set; }
        public IEnumerable<SelectListItem>? PropertyTypes { get; set; }
        public List<Amenity>? AvailableAmenities { get; set; }

        // 4. Dùng riêng cho chức năng Edit để hiển thị ảnh cũ
        public List<AccommodationImage>? ExistingImages { get; set; }
        [Range(-90, 90, ErrorMessage = "Vĩ độ phải nằm trong khoảng -90 đến 90")]
        [Display(Name = "Vĩ độ")]
        public decimal? Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Kinh độ phải nằm trong khoảng -180 đến 180")]
        [Display(Name = "Kinh độ")]
        public decimal? Longitude { get; set; }
    }
}