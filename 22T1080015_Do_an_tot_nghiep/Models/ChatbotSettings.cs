namespace _22T1080015_Do_an_tot_nghiep.Models
{
    public class ChatbotSettings
    {
        public string SystemPrompt { get; set; } = string.Empty;

        public string OutOfScopeMessage { get; set; } =
            "Xin lỗi, tôi chỉ có thể hỗ trợ các câu hỏi liên quan đến nơi lưu trú, du lịch và thông tin trong hệ thống.";

        public List<string> AllowedKeywords { get; set; } = new List<string>();
    }
}