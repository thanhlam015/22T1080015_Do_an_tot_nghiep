using _22T1080015_Do_an_tot_nghiep.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace _22T1080015_Do_an_tot_nghiep.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AISettings _aiSettings;
        private readonly ChatbotSettings _chatbotSettings;
        private readonly DoAnTotNghiepContext _context;
        private readonly IEmbeddingService _embeddingService;

        public ChatbotService(
            IHttpClientFactory httpClientFactory,
            IOptions<AISettings> aiSettings,
            IOptions<ChatbotSettings> chatbotSettings,
            DoAnTotNghiepContext context,
            IEmbeddingService embeddingService)
        {
            _httpClientFactory = httpClientFactory;
            _aiSettings = aiSettings.Value;
            _chatbotSettings = chatbotSettings.Value;
            _context = context;
            _embeddingService = embeddingService;
        }

        public async Task<string> AskAsync(string question)
        {
            if (!IsQuestionInScope(question))
            {
                return GetOutOfScopeMessage();
            }

            var ragContext = await SearchRagContextAsync(question);

            if (string.IsNullOrWhiteSpace(ragContext))
            {
                return "Hệ thống chưa có dữ liệu phù hợp để trả lời câu hỏi này. Bạn có thể thử hỏi rõ hơn về khu vực, giá phòng, loại nơi lưu trú hoặc tiện nghi.";
            }

            // Nếu UseMockChat = true thì không gọi Gemini nữa.
            // Bot sẽ trả lời trực tiếp dựa trên dữ liệu RAG/Vector DB đã lấy được.
            if (_aiSettings.UseMockChat)
            {
                return GenerateAnswerFromRagContext(question, ragContext);
            }

            try
            {
                return await AskGeminiAsync(question, ragContext);
            }
            catch (Exception ex)
            {
                var message = ex.Message.ToLower();

                if (message.Contains("403") ||
                    message.Contains("429") ||
                    message.Contains("permission_denied") ||
                    message.Contains("resource_exhausted") ||
                    message.Contains("quota"))
                {
                    return GenerateAnswerFromRagContext(question, ragContext);
                }

                throw;
            }
        }
        public async Task<UserChatbotAnswerVM> AskWithCardsAsync(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return new UserChatbotAnswerVM
                {
                    Answer = "Vui lòng nhập câu hỏi."
                };
            }

            question = question.Trim();

            if (IsPrivateQuestion(question))
            {
                return new UserChatbotAnswerVM
                {
                    Answer = "Xin lỗi, tôi không thể cung cấp thông tin riêng tư như dữ liệu admin, nhân viên, khách hàng, booking, thanh toán, email, số điện thoại, mật khẩu, SQL hoặc API key."
                };
            }

            var publicAnswer = TryAnswerPublicPolicyQuestion(question);

            var intent = await DetectSearchIntentAsync(question);

            if (publicAnswer != null && !intent.HasAccommodationSearchIntent)
            {
                return new UserChatbotAnswerVM
                {
                    Answer = publicAnswer
                };
            }

            if (!intent.HasAccommodationSearchIntent && !IsQuestionInScope(question))
            {
                return new UserChatbotAnswerVM
                {
                    Answer = GetOutOfScopeMessage()
                };
            }

            if (intent.HasAccommodationSearchIntent)
            {
                var cards = await GetAccommodationCardsByIntentAsync(intent);

                if (cards.Any())
                {
                    return new UserChatbotAnswerVM
                    {
                        Answer = BuildCardAnswerText(intent, cards),
                        Cards = cards
                    };
                }

                return new UserChatbotAnswerVM
                {
                    Answer = "Hiện hệ thống chưa có nơi lưu trú phù hợp với điều kiện bạn đưa ra. Bạn có thể thử giảm bớt điều kiện lọc, đổi khu vực hoặc tăng khoảng giá."
                };
            }

            var answer = await AskAsync(question);

            return new UserChatbotAnswerVM
            {
                Answer = answer
            };
        }

        public async Task<string> AskWithContextAsync(string question, string ragContext)
        {
            if (!IsQuestionInScope(question))
            {
                return GetOutOfScopeMessage();
            }

            if (string.IsNullOrWhiteSpace(ragContext))
            {
                ragContext = "Không tìm thấy dữ liệu nội bộ phù hợp trong hệ thống.";
            }

            if (_aiSettings.UseMockChat)
            {
                return GenerateAnswerFromRagContext(question, ragContext);
            }

            try
            {
                return await AskGeminiAsync(question, ragContext);
            }
            catch (Exception ex)
            {
                var message = ex.Message.ToLower();

                if (message.Contains("403") ||
                    message.Contains("429") ||
                    message.Contains("permission_denied") ||
                    message.Contains("resource_exhausted") ||
                    message.Contains("quota"))
                {
                    return GenerateAnswerFromRagContext(question, ragContext);
                }

                throw;
            }
        }

        public async Task<string> AskGeminiAsync(string question, string ragContext)
        {
            if (string.IsNullOrWhiteSpace(_aiSettings.GeminiApiKey))
            {
                throw new InvalidOperationException("Chưa cấu hình Gemini API key trong appsettings.json.");
            }

            var client = _httpClientFactory.CreateClient();

            var model = string.IsNullOrWhiteSpace(_aiSettings.GeminiChatModel)
                                ? "gemini-2.0-flash"
                                : _aiSettings.GeminiChatModel;

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

            var systemPrompt = GetSystemPrompt();

            var fullPrompt = $@"
                                Dữ liệu nội bộ được phép sử dụng:
                                {ragContext}

                                Câu hỏi của người dùng:
                                {question}

                                Yêu cầu trả lời:
                                - Chỉ dựa vào dữ liệu nội bộ nếu có.
                                - Nếu dữ liệu nội bộ không có thông tin phù hợp, hãy nói rõ là hệ thống chưa có dữ liệu phù hợp.
                                - Không tự bịa thông tin.
                                ";

            var body = new
            {
                systemInstruction = new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = systemPrompt
                        }
                    }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new
                            {
                                text = fullPrompt
                            }
                        }
                    }
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("x-goog-api-key", _aiSettings.GeminiApiKey);

            request.Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.SendAsync(request);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException("Bot lỗi: " + json);
            }

            using var doc = JsonDocument.Parse(json);

            var root = doc.RootElement;

            if (!root.TryGetProperty("candidates", out var candidates) ||
                candidates.GetArrayLength() == 0)
            {
                return "Bot chưa tạo được câu trả lời phù hợp.";
            }

            var firstCandidate = candidates[0];

            if (!firstCandidate.TryGetProperty("content", out var content) ||
                !content.TryGetProperty("parts", out var parts) ||
                parts.GetArrayLength() == 0)
            {
                return "Bot chưa tạo được nội dung trả lời.";
            }

            if (parts[0].TryGetProperty("text", out var text))
            {
                return text.GetString() ?? "Bot chưa có câu trả lời.";
            }

            return "Bot chưa có câu trả lời.";
        }
        private bool IsPrivateQuestion(string question)
        {
            var q = question.ToLower().Trim();

            var privateKeywords = new[]
            {
                "admin",
                "quản trị",
                "nhân viên",
                "staff",
                "employee",
                "mật khẩu",
                "password",
                "api key",
                "apikey",
                "token",
                "secret",
                "connection string",
                "sql",
                "database",
                "cơ sở dữ liệu",
                "email khách",
                "email của khách",
                "số điện thoại khách",
                "sdt khách",
                "thông tin khách hàng",
                "dữ liệu khách hàng",
                "khách hàng khác",
                "người dùng khác",
                "booking của khách",
                "đơn của khách",
                "đặt phòng của khách",
                "thanh toán của khách",
                "giao dịch của khách",
                "danh sách khách",
                "danh sách user",
                "tài khoản người dùng",
                "tài khoản khách hàng"
            };

            return privateKeywords.Any(k => q.Contains(k));
        }
        private string? TryAnswerPublicPolicyQuestion(string question)
        {
            var q = question.ToLower();

            if (q.Contains("thanh toán") ||
                q.Contains("qr") ||
                q.Contains("tiền mặt") ||
                q.Contains("chuyển khoản"))
            {
                return "Hệ thống hiện hỗ trợ 2 phương thức thanh toán: thanh toán tiền mặt tại nơi lưu trú và chuyển khoản bằng mã QR. Với QR, người dùng cần quét mã bằng ứng dụng ngân hàng và xác nhận chuyển khoản. Sau đó Admin sẽ kiểm tra giao dịch và cập nhật trạng thái thanh toán.";
            }

            if (q.Contains("đánh giá") ||
                q.Contains("review") ||
                q.Contains("bình luận"))
            {
                return "Người dùng chỉ có thể đánh giá nơi lưu trú sau khi đã đặt phòng và hoàn thành thời gian lưu trú. Nếu chưa từng đặt hoặc đơn chưa hoàn tất, hệ thống sẽ không cho gửi đánh giá.";
            }

            if (q.Contains("đặt phòng") ||
                q.Contains("cách đặt") ||
                q.Contains("book phòng") ||
                q.Contains("booking"))
            {
                return "Để đặt phòng, bạn chọn nơi lưu trú, chọn ngày nhận/trả phòng, chọn số lượng phòng phù hợp rồi bấm Đặt phòng. Sau đó bạn nhập thông tin người đặt và chọn phương thức thanh toán.";
            }

            if (q.Contains("hủy") ||
                q.Contains("huỷ") ||
                q.Contains("cancel"))
            {
                return "Việc hủy đặt phòng phụ thuộc vào trạng thái đơn và chính sách của nơi lưu trú. Bạn có thể vào mục đơn đặt phòng của mình để kiểm tra trạng thái và thao tác hủy nếu hệ thống cho phép.";
            }

            return null;
        }
        private class ChatbotSearchIntent
        {
            public string? Area { get; set; }

            public int? PropertyTypeId { get; set; }

            public string? PropertyTypeName { get; set; }

            public decimal? MinPrice { get; set; }

            public decimal? MaxPrice { get; set; }

            public double? MinRating { get; set; }

            public int? StarRating { get; set; }

            public int? GuestCount { get; set; }

            public string? BedType { get; set; }

            public bool HasPromotion { get; set; }

            public bool OnlyFeatured { get; set; }

            public List<int> AccommodationAmenityIds { get; set; } = new();

            public List<int> RoomAmenityIds { get; set; } = new();

            public bool HasAccommodationSearchIntent =>
                !string.IsNullOrWhiteSpace(Area) ||
                PropertyTypeId.HasValue ||
                MinPrice.HasValue ||
                MaxPrice.HasValue ||
                MinRating.HasValue ||
                StarRating.HasValue ||
                GuestCount.HasValue ||
                !string.IsNullOrWhiteSpace(BedType) ||
                HasPromotion ||
                OnlyFeatured ||
                AccommodationAmenityIds.Any() ||
                RoomAmenityIds.Any();
        }
        private async Task<ChatbotSearchIntent> DetectSearchIntentAsync(string question)
        {
            var intent = new ChatbotSearchIntent();

            var q = question.ToLower().Trim();

            intent.Area = await DetectAreaFromDatabaseAsync(q);

            DetectPriceIntent(q, intent);

            DetectRatingIntent(q, intent);

            DetectGuestIntent(q, intent);

            await DetectPropertyTypeIntentAsync(q, intent);

            await DetectAmenityIntentAsync(q, intent);

            DetectOtherIntent(q, intent);

            return intent;
        }
        private async Task<string?> DetectAreaFromDatabaseAsync(string question)
        {
            var districts = await _context.Districts
                .OrderByDescending(d => d.Name.Length)
                .Select(d => d.Name)
                .ToListAsync();

            foreach (var district in districts)
            {
                if (question.Contains(district.ToLower()))
                {
                    return district;
                }
            }

            var commonAreas = new[]
            {
        "đà nẵng",
        "huế",
        "đà lạt",
        "hội an",
        "nha trang",
        "phú quốc",
        "hà nội",
        "sa pa",
        "sapa",
        "sài gòn",
        "tp. hồ chí minh"
    };

            return commonAreas.FirstOrDefault(question.Contains);
        }

        private void DetectPriceIntent(string question, ChatbotSearchIntent intent)
        {
            if (question.Contains("giá rẻ") ||
                question.Contains("rẻ nhất") ||
                question.Contains("tiết kiệm"))
            {
                intent.MaxPrice ??= 1000000;
            }

            if (question.Contains("dưới 500k") ||
                question.Contains("dưới 500 nghìn") ||
                question.Contains("duoi 500k"))
            {
                intent.MaxPrice = 500000;
            }
            else if (question.Contains("dưới 1 triệu") ||
                     question.Contains("duoi 1 trieu") ||
                     question.Contains("dưới 1000000"))
            {
                intent.MaxPrice = 1000000;
            }
            else if (question.Contains("dưới 2 triệu") ||
                     question.Contains("duoi 2 trieu") ||
                     question.Contains("dưới 2000000"))
            {
                intent.MaxPrice = 2000000;
            }
            else if (question.Contains("dưới 3 triệu") ||
                     question.Contains("duoi 3 trieu"))
            {
                intent.MaxPrice = 3000000;
            }

            var matches = System.Text.RegularExpressions.Regex.Matches(
                question,
                @"\d+([,.]\d+)?\s*(triệu|trieu|k|nghìn|ngan)?"
            );

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var rawNumber = match.Groups[0].Value.ToLower();

                decimal value = ParseMoneyText(rawNumber);

                if (value <= 0)
                {
                    continue;
                }

                if (question.Contains("dưới") ||
                    question.Contains("duoi") ||
                    question.Contains("tối đa") ||
                    question.Contains("toi da"))
                {
                    intent.MaxPrice = value;
                }
            }
        }

        private decimal ParseMoneyText(string text)
        {
            text = text.ToLower().Trim();

            bool isMillion = text.Contains("triệu") || text.Contains("trieu");
            bool isThousand = text.Contains("k") || text.Contains("nghìn") || text.Contains("ngan");

            var numberText = text
                .Replace("triệu", "")
                .Replace("trieu", "")
                .Replace("nghìn", "")
                .Replace("ngan", "")
                .Replace("k", "")
                .Trim();

            numberText = numberText.Replace(",", ".");

            if (!decimal.TryParse(
                    numberText,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var number))
            {
                return 0;
            }

            if (isMillion)
            {
                return number * 1000000;
            }

            if (isThousand)
            {
                return number * 1000;
            }

            if (number < 10000)
            {
                return number * 1000000;
            }

            return number;
        }

        private void DetectRatingIntent(string question, ChatbotSearchIntent intent)
        {
            if (question.Contains("đánh giá cao") ||
                question.Contains("rating cao") ||
                question.Contains("tốt nhất"))
            {
                intent.MinRating = 8;
            }

            if (question.Contains("trên 9") || question.Contains("từ 9"))
            {
                intent.MinRating = 9;
            }
            else if (question.Contains("trên 8") || question.Contains("từ 8"))
            {
                intent.MinRating = 8;
            }
            else if (question.Contains("trên 7") || question.Contains("từ 7"))
            {
                intent.MinRating = 7;
            }

            if (question.Contains("5 sao"))
            {
                intent.StarRating = 5;
            }
            else if (question.Contains("4 sao"))
            {
                intent.StarRating = 4;
            }
            else if (question.Contains("3 sao"))
            {
                intent.StarRating = 3;
            }
        }

        private void DetectGuestIntent(string question, ChatbotSearchIntent intent)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                question,
                @"(\d+)\s*(người|khách)"
            );

            if (match.Success &&
                int.TryParse(match.Groups[1].Value, out int guestCount) &&
                guestCount > 0)
            {
                intent.GuestCount = guestCount;
            }

            if (question.Contains("gia đình"))
            {
                intent.GuestCount ??= 4;
            }
        }

        private async Task DetectPropertyTypeIntentAsync(string question, ChatbotSearchIntent intent)
        {
            var types = await _context.PropertyTypes
                .OrderByDescending(p => p.NamePropertyTypes.Length)
                .ToListAsync();

            foreach (var type in types)
            {
                var typeName = type.NamePropertyTypes.ToLower();

                if (question.Contains(typeName))
                {
                    intent.PropertyTypeId = type.Id;
                    intent.PropertyTypeName = type.NamePropertyTypes;
                    return;
                }
            }

            if (question.Contains("khách sạn"))
            {
                var type = types.FirstOrDefault(x => x.NamePropertyTypes.ToLower().Contains("khách sạn"));
                intent.PropertyTypeId = type?.Id;
                intent.PropertyTypeName = type?.NamePropertyTypes;
            }
            else if (question.Contains("resort") || question.Contains("nghỉ dưỡng"))
            {
                var type = types.FirstOrDefault(x => x.NamePropertyTypes.ToLower().Contains("resort"));
                intent.PropertyTypeId = type?.Id;
                intent.PropertyTypeName = type?.NamePropertyTypes;
            }
            else if (question.Contains("homestay"))
            {
                var type = types.FirstOrDefault(x => x.NamePropertyTypes.ToLower().Contains("homestay"));
                intent.PropertyTypeId = type?.Id;
                intent.PropertyTypeName = type?.NamePropertyTypes;
            }
            else if (question.Contains("villa") || question.Contains("biệt thự"))
            {
                var type = types.FirstOrDefault(x =>
                    x.NamePropertyTypes.ToLower().Contains("villa") ||
                    x.NamePropertyTypes.ToLower().Contains("biệt thự"));

                intent.PropertyTypeId = type?.Id;
                intent.PropertyTypeName = type?.NamePropertyTypes;
            }
            else if (question.Contains("căn hộ") || question.Contains("apartment"))
            {
                var type = types.FirstOrDefault(x =>
                    x.NamePropertyTypes.ToLower().Contains("căn hộ") ||
                    x.NamePropertyTypes.ToLower().Contains("apartment"));

                intent.PropertyTypeId = type?.Id;
                intent.PropertyTypeName = type?.NamePropertyTypes;
            }
        }

        private async Task DetectAmenityIntentAsync(string question, ChatbotSearchIntent intent)
        {
            var amenities = await _context.Amenities.ToListAsync();

            foreach (var amenity in amenities)
            {
                var name = amenity.Name.ToLower();

                if (question.Contains(name))
                {
                    if (amenity.Category == "Room")
                    {
                        intent.RoomAmenityIds.Add(amenity.Id);
                    }
                    else
                    {
                        intent.AccommodationAmenityIds.Add(amenity.Id);
                    }
                }
            }

            AddAmenityByKeyword(question, amenities, intent, "wifi", "wifi");
            AddAmenityByKeyword(question, amenities, intent, "hồ bơi", "hồ bơi");
            AddAmenityByKeyword(question, amenities, intent, "bể bơi", "hồ bơi");
            AddAmenityByKeyword(question, amenities, intent, "spa", "spa");
            AddAmenityByKeyword(question, amenities, intent, "đưa đón sân bay", "sân bay");
            AddAmenityByKeyword(question, amenities, intent, "bãi đỗ xe", "đỗ xe");
            AddAmenityByKeyword(question, amenities, intent, "bãi đậu xe", "đậu xe");
            AddAmenityByKeyword(question, amenities, intent, "bữa sáng", "sáng");
            AddAmenityByKeyword(question, amenities, intent, "buffet", "buffet");

            AddRoomAmenityByKeyword(question, amenities, intent, "bồn tắm", "bồn tắm");
            AddRoomAmenityByKeyword(question, amenities, intent, "ban công", "ban công");
            AddRoomAmenityByKeyword(question, amenities, intent, "view biển", "view biển");
            AddRoomAmenityByKeyword(question, amenities, intent, "máy lạnh", "máy lạnh");
            AddRoomAmenityByKeyword(question, amenities, intent, "tivi", "tivi");

            if (question.Contains("giường đôi"))
            {
                intent.BedType = "giường đôi";
            }
            else if (question.Contains("2 giường"))
            {
                intent.BedType = "2 giường";
            }
        }

        private void AddAmenityByKeyword(
            string question,
            List<Amenity> amenities,
            ChatbotSearchIntent intent,
            string keyword,
            string amenityNameContains)
        {
            if (!question.Contains(keyword))
            {
                return;
            }

            var matched = amenities
                .Where(a =>
                    a.Category != "Room" &&
                    a.Name.ToLower().Contains(amenityNameContains))
                .Select(a => a.Id)
                .ToList();

            intent.AccommodationAmenityIds.AddRange(matched);
            intent.AccommodationAmenityIds = intent.AccommodationAmenityIds.Distinct().ToList();
        }

        private void AddRoomAmenityByKeyword(
            string question,
            List<Amenity> amenities,
            ChatbotSearchIntent intent,
            string keyword,
            string amenityNameContains)
        {
            if (!question.Contains(keyword))
            {
                return;
            }

            var matched = amenities
                .Where(a =>
                    a.Category == "Room" &&
                    a.Name.ToLower().Contains(amenityNameContains))
                .Select(a => a.Id)
                .ToList();

            intent.RoomAmenityIds.AddRange(matched);
            intent.RoomAmenityIds = intent.RoomAmenityIds.Distinct().ToList();
        }

        private void DetectOtherIntent(string question, ChatbotSearchIntent intent)
        {
            if (question.Contains("khuyến mãi") ||
                question.Contains("ưu đãi") ||
                question.Contains("giảm giá") ||
                question.Contains("voucher"))
            {
                intent.HasPromotion = true;
            }

            if (question.Contains("nổi bật") ||
                question.Contains("đề xuất"))
            {
                intent.OnlyFeatured = true;
            }
        }
        private async Task<List<UserChatbotAccommodationCardVM>> GetAccommodationCardsByIntentAsync(ChatbotSearchIntent intent)
        {
            var now = DateTime.Now;

            var activePromotionAccommodationIds = await _context.PromotionAccommodations
                .Where(pa =>
                    pa.Promotion.Status == "Active" &&
                    pa.Promotion.StartDate <= now &&
                    pa.Promotion.EndDate >= now &&
                    (!pa.Promotion.UsageLimit.HasValue || pa.Promotion.UsedCount < pa.Promotion.UsageLimit.Value))
                .Select(pa => pa.AccommodationId)
                .Distinct()
                .ToListAsync();

            var accommodations = await _context.Accommodations
                .Include(a => a.District)
                .Include(a => a.PropertyType)
                .Include(a => a.AccommodationImages)
                .Include(a => a.AccommodationAmenities)
                    .ThenInclude(aa => aa.Amenity)
                .Include(a => a.Rooms)
                    .ThenInclude(r => r.Bookings)
                .Include(a => a.Rooms)
                    .ThenInclude(r => r.Amenities)
                .Where(a => a.Status == "Active")
                .ToListAsync();

            if (!string.IsNullOrWhiteSpace(intent.Area))
            {
                var area = intent.Area.ToLower();

                accommodations = accommodations
                    .Where(a =>
                        a.Name.ToLower().Contains(area) ||
                        a.Address.ToLower().Contains(area) ||
                        a.District.Name.ToLower().Contains(area))
                    .ToList();
            }

            if (intent.PropertyTypeId.HasValue)
            {
                accommodations = accommodations
                    .Where(a => a.PropertyTypeId == intent.PropertyTypeId.Value)
                    .ToList();
            }

            if (intent.StarRating.HasValue)
            {
                accommodations = accommodations
                    .Where(a => a.StarRating.HasValue && a.StarRating.Value >= intent.StarRating.Value)
                    .ToList();
            }

            if (intent.MinRating.HasValue)
            {
                accommodations = accommodations
                    .Where(a =>
                        NormalizeChatRating(a.AverageRating).HasValue &&
                        NormalizeChatRating(a.AverageRating)!.Value >= intent.MinRating.Value)
                    .ToList();
            }

            if (intent.OnlyFeatured)
            {
                accommodations = accommodations
                    .Where(a => a.IsFeatured)
                    .ToList();
            }

            if (intent.HasPromotion)
            {
                accommodations = accommodations
                    .Where(a => activePromotionAccommodationIds.Contains(a.Id))
                    .ToList();
            }

            if (intent.AccommodationAmenityIds.Any())
            {
                accommodations = accommodations
                    .Where(a =>
                        intent.AccommodationAmenityIds.All(id =>
                            a.AccommodationAmenities.Any(aa => aa.AmenityId == id)))
                    .ToList();
            }

            var cards = new List<UserChatbotAccommodationCardVM>();

            foreach (var accommodation in accommodations)
            {
                var activeRooms = accommodation.Rooms
                    .Where(r => !r.IsDeleted && r.Status == "Active")
                    .ToList();

                if (intent.GuestCount.HasValue)
                {
                    activeRooms = activeRooms
                        .Where(r => r.Capacity >= intent.GuestCount.Value)
                        .ToList();
                }

                if (!string.IsNullOrWhiteSpace(intent.BedType))
                {
                    var bedType = intent.BedType.ToLower();

                    activeRooms = activeRooms
                        .Where(r =>
                            !string.IsNullOrWhiteSpace(r.BedType) &&
                            r.BedType.ToLower().Contains(bedType))
                        .ToList();
                }

                if (intent.RoomAmenityIds.Any())
                {
                    activeRooms = activeRooms
                        .Where(r =>
                            intent.RoomAmenityIds.All(id =>
                                r.Amenities.Any(a => a.Id == id)))
                        .ToList();
                }

                if (!activeRooms.Any())
                {
                    continue;
                }

                decimal minPrice = activeRooms.Min(r => r.PriceNight);

                if (intent.MinPrice.HasValue && minPrice < intent.MinPrice.Value)
                {
                    continue;
                }

                if (intent.MaxPrice.HasValue && minPrice > intent.MaxPrice.Value)
                {
                    continue;
                }

                int bookingCount = activeRooms
                    .SelectMany(r => r.Bookings)
                    .Where(b => b.Status != "Cancelled" && b.Status != "Canceled")
                    .Sum(b => b.NumberOfRooms);

                string? promotionText = activePromotionAccommodationIds.Contains(accommodation.Id)
                    ? "Đang có ưu đãi"
                    : null;

                cards.Add(new UserChatbotAccommodationCardVM
                {
                    Id = accommodation.Id,
                    Name = accommodation.Name,
                    DistrictName = accommodation.District?.Name,
                    PropertyTypeName = accommodation.PropertyType?.NamePropertyTypes,
                    MinPrice = minPrice,
                    AverageRating = NormalizeChatRating(accommodation.AverageRating),
                    BookingCount = bookingCount,
                    PromotionText = promotionText,
                    ImageUrl = accommodation.AccommodationImages
                        .OrderByDescending(i => i.IsPrimary)
                        .ThenBy(i => i.SortOrder ?? int.MaxValue)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault(),
                    DetailUrl = $"/Users/Details?id={accommodation.Id}"
                });
            }

            return cards
                .OrderByDescending(x => x.BookingCount)
                .ThenByDescending(x => x.AverageRating ?? 0)
                .ThenBy(x => x.MinPrice)
                .Take(5)
                .ToList();
        }
        private string BuildCardAnswerText(
            ChatbotSearchIntent intent,
            List<UserChatbotAccommodationCardVM> cards)
        {
            var conditions = new List<string>();

            if (!string.IsNullOrWhiteSpace(intent.Area))
            {
                conditions.Add($"khu vực {intent.Area}");
            }

            if (!string.IsNullOrWhiteSpace(intent.PropertyTypeName))
            {
                conditions.Add($"loại {intent.PropertyTypeName}");
            }

            if (intent.MaxPrice.HasValue)
            {
                conditions.Add($"giá dưới {intent.MaxPrice.Value:N0}đ/đêm");
            }

            if (intent.MinRating.HasValue)
            {
                conditions.Add($"đánh giá từ {intent.MinRating.Value:0.#}");
            }

            if (intent.StarRating.HasValue)
            {
                conditions.Add($"từ {intent.StarRating.Value} sao");
            }

            if (intent.HasPromotion)
            {
                conditions.Add("đang có ưu đãi");
            }

            if (intent.AccommodationAmenityIds.Any() || intent.RoomAmenityIds.Any())
            {
                conditions.Add("có tiện nghi phù hợp");
            }

            string conditionText = conditions.Any()
                ? string.Join(", ", conditions)
                : "phù hợp với yêu cầu của bạn";

            return $"Tôi tìm thấy {cards.Count} nơi lưu trú {conditionText}. Danh sách được ưu tiên theo lượng người đã đặt, sau đó đến điểm đánh giá.";
        }

        private double? NormalizeChatRating(double? rating)
        {
            if (!rating.HasValue)
            {
                return null;
            }

            if (rating.Value <= 5)
            {
                return Math.Round(rating.Value * 2, 1);
            }

            return Math.Round(rating.Value, 1);
        }
        private async Task<string> SearchRagContextAsync(string question)
        {
            var chunks = await _context.RagChunks
                .Include(c => c.Document)
                .Where(c =>
                    c.Document.IsActive &&
                    c.Document.AiIndexStatus == "Indexed")
                .Take(1000)
                .ToListAsync();

            if (!chunks.Any())
            {
                return "";
            }

            float[]? questionEmbedding = null;

            try
            {
                questionEmbedding = await _embeddingService.CreateEmbeddingAsync(question);
            }
            catch
            {
                questionEmbedding = null;
            }

            var keywords = ExtractKeywords(question);

            var ranked = chunks
                .Select(chunk =>
                {
                    double keywordScore = CalculateKeywordScore(
                        question,
                        keywords,
                        chunk.Content,
                        chunk.Document.Title);

                    double vectorScore = 0;

                    if (questionEmbedding != null &&
                        !string.IsNullOrWhiteSpace(chunk.EmbeddingJson))
                    {
                        var chunkEmbedding = TryParseEmbedding(chunk.EmbeddingJson);

                        if (chunkEmbedding != null &&
                            chunkEmbedding.Length == questionEmbedding.Length)
                        {
                            vectorScore = CosineSimilarity(questionEmbedding, chunkEmbedding);
                        }
                    }

                    return new
                    {
                        Chunk = chunk,
                        Score = keywordScore + vectorScore
                    };
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(6)
                .ToList();

            if (!ranked.Any())
            {
                ranked = chunks
                    .Select(chunk => new
                    {
                        Chunk = chunk,
                        Score = CalculateKeywordScore(
                            question,
                            keywords,
                            chunk.Content,
                            chunk.Document.Title)
                    })
                    .OrderByDescending(x => x.Score)
                    .Take(4)
                    .ToList();
            }

            if (!ranked.Any())
            {
                return "";
            }

            var sb = new StringBuilder();

            foreach (var item in ranked)
            {
                sb.AppendLine($"Tài liệu: {item.Chunk.Document.Title}");
                sb.AppendLine(item.Chunk.Content);
                sb.AppendLine("-----");
            }

            return sb.ToString();
        }

        private List<string> ExtractKeywords(string question)
        {
            var stopWords = new HashSet<string>
    {
        "cho", "tôi", "mot", "một", "so", "số", "cac", "các",
        "voi", "với", "gia", "giá", "duoi", "dưới", "tren", "trên",
        "o", "ở", "la", "là", "co", "có", "khong", "không",
        "noi", "nơi", "luu", "lưu", "tru", "trú", "phong", "phòng"
    };

            var normalized = question.ToLower()
                .Replace(",", " ")
                .Replace(".", " ")
                .Replace("?", " ")
                .Replace("!", " ")
                .Replace(":", " ")
                .Replace(";", " ");

            var words = normalized
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 2)
                .Where(w => !stopWords.Contains(w))
                .Distinct()
                .ToList();

            return words;
        }

        private double CalculateKeywordScore(
            string question,
            List<string> keywords,
            string content,
            string title)
        {
            double score = 0;

            var lowerContent = content.ToLower();
            var lowerTitle = title.ToLower();
            var lowerQuestion = question.ToLower();

            foreach (var keyword in keywords)
            {
                if (lowerTitle.Contains(keyword))
                {
                    score += 5;
                }

                if (lowerContent.Contains(keyword))
                {
                    score += 3;
                }
            }

            if (lowerQuestion.Contains("huế") &&
                (lowerContent.Contains("huế") || lowerTitle.Contains("huế")))
            {
                score += 15;
            }

            if (lowerQuestion.Contains("đà nẵng") &&
                (lowerContent.Contains("đà nẵng") || lowerTitle.Contains("đà nẵng")))
            {
                score += 15;
            }

            if ((lowerQuestion.Contains("dưới 1 triệu") ||
                 lowerQuestion.Contains("duoi 1 trieu") ||
                 lowerQuestion.Contains("dưới 1000000") ||
                 lowerQuestion.Contains("duoi 1000000")) &&
                (lowerContent.Contains("900,000") ||
                 lowerContent.Contains("850,000") ||
                 lowerContent.Contains("750,000") ||
                 lowerContent.Contains("650,000") ||
                 lowerContent.Contains("450,000") ||
                 lowerContent.Contains("420,000")))
            {
                score += 8;
            }

            return score;
        }

        private float[]? TryParseEmbedding(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<float[]>(json);
            }
            catch
            {
                return null;
            }
        }

        private double CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length)
            {
                return 0;
            }

            double dot = 0;
            double normA = 0;
            double normB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            if (normA == 0 || normB == 0)
            {
                return 0;
            }

            return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }

        private bool IsQuestionInScope(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return false;
            }

            question = question.ToLower().Trim();

            var blockedKeywords = new[]
            {
                "admin",
                "nhân viên",
                "mật khẩu",
                "password",
                "tài khoản",
                "email khách",
                "số điện thoại",
                "thanh toán",
                "payment",
                "booking của khách",
                "dữ liệu khách hàng",
                "database",
                "sql",
                "connection string"
            };

            if (blockedKeywords.Any(k => question.Contains(k)))
            {
                return false;
            }

            if (_chatbotSettings.AllowedKeywords != null &&
                _chatbotSettings.AllowedKeywords.Any())
            {
                return _chatbotSettings.AllowedKeywords
                    .Any(k => question.Contains(k.ToLower()));
            }

            var allowedKeywords = new[]
            {
                "khách sạn",
                "nơi lưu trú",
                "homestay",
                "resort",
                "villa",
                "căn hộ",
                "phòng",
                "giá",
                "tiện nghi",
                "hồ bơi",
                "wifi",
                "spa",
                "bữa sáng",
                "giường",
                "view",
                "ban công",
                "bồn tắm",
                "địa điểm",
                "khu vực",
                "du lịch",
                "khuyến mãi",
                "ưu đãi",
                "đặt phòng",
                "thanh toán",
                "qr",
                "tiền mặt",
                "đánh giá",
                "review",
                "ở đâu",
                "nên đi đâu",
                "gợi ý"
            };

            return allowedKeywords.Any(k => question.Contains(k));
        }
        private string GenerateAnswerFromRagContext(string question, string ragContext)
        {
            var lowerQuestion = question.ToLower();

            var blocks = ragContext
                .Split("-----", StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (!blocks.Any())
            {
                return "Hệ thống chưa có dữ liệu phù hợp để trả lời câu hỏi này.";
            }

            string? areaKeyword = DetectAreaKeyword(lowerQuestion);
            decimal? maxPrice = DetectMaxPrice(lowerQuestion);

            var matchedBlocks = blocks
                .Where(block =>
                {
                    var lowerBlock = block.ToLower();

                    bool matchArea = string.IsNullOrWhiteSpace(areaKeyword) ||
                                     lowerBlock.Contains(areaKeyword);

                    bool matchPrice = true;

                    if (maxPrice.HasValue)
                    {
                        var prices = ExtractPrices(lowerBlock);

                        matchPrice = prices.Any(p => p <= maxPrice.Value);
                    }

                    return matchArea && matchPrice;
                })
                .Take(5)
                .ToList();

            if (!matchedBlocks.Any())
            {
                matchedBlocks = blocks.Take(5).ToList();
            }

            var sb = new StringBuilder();

            sb.AppendLine("Dựa trên dữ liệu hiện có trong hệ thống TravelBot, tôi gợi ý cho bạn:");

            int index = 1;

            foreach (var block in matchedBlocks)
            {
                var title = ExtractTitle(block);
                var price = ExtractFirstPrice(block);
                var location = ExtractLocation(block);

                sb.AppendLine();

                sb.AppendLine($"{index}. {title}");

                if (!string.IsNullOrWhiteSpace(location))
                {
                    sb.AppendLine($"- Khu vực/địa điểm: {location}");
                }

                if (price.HasValue)
                {
                    sb.AppendLine($"- Giá tham khảo: {price.Value:N0}đ/đêm");
                }

                var amenities = ExtractAmenities(block);

                if (!string.IsNullOrWhiteSpace(amenities))
                {
                    sb.AppendLine($"- Tiện nghi nổi bật: {amenities}");
                }

                index++;
            }

            sb.AppendLine();
            sb.AppendLine("Bạn có thể bấm vào nơi lưu trú trong danh sách để xem chi tiết phòng, giá theo ngày và đặt phòng.");

            return sb.ToString();
        }

        private string? DetectAreaKeyword(string question)
        {
            var areas = new[]
            {
        "huế",
        "đà nẵng",
        "đà lạt",
        "hội an",
        "nha trang",
        "phú quốc",
        "hà nội",
        "sài gòn",
        "tp. hồ chí minh",
        "sa pa"
    };

            return areas.FirstOrDefault(question.Contains);
        }

        private decimal? DetectMaxPrice(string question)
        {
            if (question.Contains("dưới 1 triệu") ||
                question.Contains("duoi 1 trieu") ||
                question.Contains("dưới 1000000") ||
                question.Contains("duoi 1000000"))
            {
                return 1000000;
            }

            if (question.Contains("dưới 2 triệu") ||
                question.Contains("duoi 2 trieu") ||
                question.Contains("dưới 2000000") ||
                question.Contains("duoi 2000000"))
            {
                return 2000000;
            }

            return null;
        }

        private List<decimal> ExtractPrices(string text)
        {
            var prices = new List<decimal>();

            var matches = System.Text.RegularExpressions.Regex.Matches(
                text,
                @"\d{1,3}([,.]\d{3})+|\d+"
            );

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var raw = match.Value
                    .Replace(".", "")
                    .Replace(",", "");

                if (decimal.TryParse(raw, out var price))
                {
                    if (price >= 100000)
                    {
                        prices.Add(price);
                    }
                }
            }

            return prices;
        }

        private decimal? ExtractFirstPrice(string text)
        {
            return ExtractPrices(text).FirstOrDefault();
        }

        private string ExtractTitle(string block)
        {
            var lines = block
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            var titleLine = lines.FirstOrDefault(x => x.StartsWith("Tài liệu:"));

            if (!string.IsNullOrWhiteSpace(titleLine))
            {
                return titleLine.Replace("Tài liệu:", "").Trim();
            }

            return lines.FirstOrDefault() ?? "Nơi lưu trú phù hợp";
        }

        private string? ExtractLocation(string block)
        {
            var lines = block
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            var locationLine = lines.FirstOrDefault(x =>
                x.ToLower().Contains("địa chỉ") ||
                x.ToLower().Contains("khu vực") ||
                x.ToLower().Contains("địa điểm"));

            return locationLine;
        }

        private string? ExtractAmenities(string block)
        {
            var lines = block
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .ToList();

            var amenityLine = lines.FirstOrDefault(x =>
                x.ToLower().Contains("tiện nghi") ||
                x.ToLower().Contains("wifi") ||
                x.ToLower().Contains("hồ bơi") ||
                x.ToLower().Contains("bữa sáng"));

            return amenityLine;
        }

        private string GetSystemPrompt()
        {
            if (!string.IsNullOrWhiteSpace(_chatbotSettings.SystemPrompt))
            {
                return _chatbotSettings.SystemPrompt;
            }

            return @"Bạn là chatbot tư vấn lưu trú du lịch của hệ thống TravelBot.

                    Nhiệm vụ:
                    - Tư vấn nơi lưu trú phù hợp với nhu cầu người dùng.
                    - Trả lời thông tin về nơi lưu trú, phòng, giá phòng, tiện nghi, địa điểm và khuyến mãi.
                    - Ưu tiên trả lời dựa trên dữ liệu nội bộ được cung cấp trong phần context.
                    - Nếu không đủ dữ liệu, hãy nói rõ rằng hệ thống chưa có thông tin phù hợp.

                    Giới hạn:
                    - Chỉ trả lời trong phạm vi hệ thống TravelBot.
                    - Không trả lời các chủ đề không liên quan đến lưu trú, du lịch, đặt phòng hoặc khuyến mãi.
                    - Không tiết lộ dữ liệu admin, nhân viên, khách hàng, booking, thanh toán, email, số điện thoại, mật khẩu hoặc dữ liệu nhạy cảm.
                    - Không tự bịa thông tin nếu context không có dữ liệu.

                    Nếu câu hỏi ngoài phạm vi, trả lời đúng câu:
                    Xin lỗi, tôi chỉ có thể hỗ trợ các câu hỏi liên quan đến nơi lưu trú, du lịch và thông tin trong hệ thống.";
        }

        private string GetOutOfScopeMessage()
        {
            if (!string.IsNullOrWhiteSpace(_chatbotSettings.OutOfScopeMessage))
            {
                return _chatbotSettings.OutOfScopeMessage;
            }

            return "Xin lỗi, tôi chỉ có thể hỗ trợ các câu hỏi liên quan đến nơi lưu trú, du lịch và thông tin trong hệ thống.";
        }
    }
}