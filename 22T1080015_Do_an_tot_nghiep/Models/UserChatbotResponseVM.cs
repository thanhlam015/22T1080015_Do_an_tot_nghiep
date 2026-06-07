namespace _22T1080015_Do_an_tot_nghiep.Models
{
    public class UserChatbotAnswerVM
    {
        public string Answer { get; set; } = string.Empty;

        public List<UserChatbotAccommodationCardVM> Cards { get; set; } = new();
    }

    public class UserChatbotAccommodationCardVM
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public string? DistrictName { get; set; }

        public string? PropertyTypeName { get; set; }

        public decimal MinPrice { get; set; }

        public double? AverageRating { get; set; }

        public int BookingCount { get; set; }

        public string? PromotionText { get; set; }

        public string DetailUrl { get; set; } = string.Empty;
    }
}