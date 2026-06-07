using _22T1080015_Do_an_tot_nghiep.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace _22T1080015_Do_an_tot_nghiep.Services
{
    public class RagIndexingService : IRagIndexingService
    {
        private readonly DoAnTotNghiepContext _context;
        private readonly IEmbeddingService _embeddingService;

        public RagIndexingService(
            DoAnTotNghiepContext context,
            IEmbeddingService embeddingService)
        {
            _context = context;
            _embeddingService = embeddingService;
        }

        public async Task<int> SyncAllAsync()
        {
            var accommodationIds = await _context.Accommodations
                .Select(a => a.Id)
                .ToListAsync();

            int syncedCount = 0;

            foreach (var id in accommodationIds)
            {
                bool changed = await SyncAccommodationAsync(id);

                if (changed)
                {
                    syncedCount++;
                }
            }

            syncedCount += await SyncPromotionsAsync();

            return syncedCount;
        }

        public async Task<bool> SyncAccommodationAsync(int accommodationId)
        {
            var accommodation = await _context.Accommodations
                .Include(a => a.District)
                .Include(a => a.PropertyType)
                .Include(a => a.AccommodationImages)
                .Include(a => a.Rooms)
                    .ThenInclude(r => r.Amenities)
                .FirstOrDefaultAsync(a => a.Id == accommodationId);

            if (accommodation == null)
            {
                return false;
            }

            try
            {
                var content = BuildAccommodationContent(accommodation);
                var title = $"Nơi lưu trú: {accommodation.Name}";

                bool changed = await UpsertDocumentAsync(
                    sourceTable: "Accommodations",
                    sourceId: accommodation.Id,
                    title: title,
                    content: content
                );

                if (changed)
                {
                    accommodation.AiIndexStatus = "Indexed";
                    accommodation.AiLastIndexedAt = DateTime.Now;
                    accommodation.AiIndexError = null;

                    await _context.SaveChangesAsync();
                }

                return changed;
            }
            catch (Exception ex)
            {
                accommodation.AiIndexStatus = "Error";
                accommodation.AiIndexError = ex.Message.Length > 500
                    ? ex.Message.Substring(0, 500)
                    : ex.Message;

                await _context.SaveChangesAsync();

                throw;
            }
        }

        private async Task<int> SyncPromotionsAsync()
        {
            var promotions = await _context.Promotions
                .Include(p => p.PromotionAccommodations)
                    .ThenInclude(pa => pa.Accommodation)
                .Where(p => p.Status == "Active")
                .ToListAsync();

            int syncedCount = 0;

            foreach (var promotion in promotions)
            {
                var content = BuildPromotionContent(promotion);

                bool changed = await UpsertDocumentAsync(
                    sourceTable: "Promotions",
                    sourceId: promotion.Id,
                    title: $"Khuyến mãi: {promotion.Title}",
                    content: content
                );

                if (changed)
                {
                    syncedCount++;
                }
            }

            return syncedCount;
        }

        private async Task<bool> UpsertDocumentAsync(
    string sourceTable,
    int sourceId,
    string title,
    string content)
        {
            var hash = CreateHash(content);

            var document = await _context.RagDocuments
                .Include(d => d.RagChunks)
                .FirstOrDefaultAsync(d =>
                    d.SourceTable == sourceTable &&
                    d.SourceId == sourceId);

            // Nếu nội dung không đổi và đã có chunks thì không cần đồng bộ lại
            if (document != null &&
                document.ContentHash == hash &&
                document.AiIndexStatus == "Indexed" &&
                document.RagChunks != null &&
                document.RagChunks.Any())
            {
                return false;
            }

            if (document == null)
            {
                document = new RagDocument
                {
                    SourceTable = sourceTable,
                    SourceId = sourceId,
                    Title = title,
                    CreatedAt = DateTime.Now,
                    IsActive = true,
                    AiIndexStatus = "Indexing"
                };

                _context.RagDocuments.Add(document);
                await _context.SaveChangesAsync();
            }

            document.Title = title;
            document.AiIndexStatus = "Indexing";
            document.ErrorMessage = null;
            document.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            try
            {
                if (document.RagChunks != null && document.RagChunks.Any())
                {
                    _context.RagChunks.RemoveRange(document.RagChunks);
                    await _context.SaveChangesAsync();
                }

                var chunks = SplitIntoChunks(content, 1200);

                if (!chunks.Any())
                {
                    document.AiIndexStatus = "Error";
                    document.ErrorMessage = "Không có nội dung để tạo chunks.";
                    document.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    return false;
                }

                for (int i = 0; i < chunks.Count; i++)
                {
                    var embedding = await _embeddingService.CreateEmbeddingAsync(chunks[i]);

                    _context.RagChunks.Add(new RagChunk
                    {
                        DocumentId = document.Id,
                        ChunkIndex = i,
                        Content = chunks[i],
                        EmbeddingJson = JsonSerializer.Serialize(embedding),
                        EmbeddingModel = _embeddingService.GetModelName(),
                        TokenCount = EstimateTokenCount(chunks[i]),
                        CreatedAt = DateTime.Now
                    });
                }

                document.ContentHash = hash;
                document.AiIndexStatus = "Indexed";
                document.ErrorMessage = null;
                document.IndexedAt = DateTime.Now;
                document.UpdatedAt = DateTime.Now;
                document.IsActive = true;

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                document.AiIndexStatus = "Error";
                document.ErrorMessage = ex.Message.Length > 500
                    ? ex.Message.Substring(0, 500)
                    : ex.Message;
                document.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                throw;
            }
        }

        private string BuildAccommodationContent(Accommodation a)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Tên nơi lưu trú: {a.Name}");
            sb.AppendLine($"Địa chỉ: {a.Address}");
            sb.AppendLine($"Quận/Huyện: {a.District?.Name}");
            sb.AppendLine($"Loại lưu trú: {a.PropertyType?.NamePropertyTypes}");
            sb.AppendLine($"Mô tả: {a.Description}");
            sb.AppendLine($"Tọa độ: {a.Latitude}, {a.Longitude}");

            if (a.AccommodationImages != null && a.AccommodationImages.Any())
            {
                sb.AppendLine("Ảnh nơi lưu trú:");

                foreach (var img in a.AccommodationImages)
                {
                    sb.AppendLine($"- {img.ImageUrl}");
                }
            }

            if (a.Rooms != null && a.Rooms.Any())
            {
                sb.AppendLine("Danh sách phòng:");

                foreach (var room in a.Rooms)
                {
                    if (room.IsDeleted || room.Status != "Active")
                    {
                        continue;
                    }

                    sb.AppendLine($"- Phòng: {room.RoomType}");
                    sb.AppendLine($"  Giá mỗi đêm: {room.PriceNight:N0}đ");
                    sb.AppendLine($"  Sức chứa tổng: {room.Capacity} người");
                    sb.AppendLine($"  Người lớn: {room.AdultCapacity}, Trẻ em: {room.ChildCapacity}");
                    sb.AppendLine($"  Tổng số phòng: {room.TotalRooms}");
                    sb.AppendLine($"  Diện tích: {room.RoomSize} m2");
                    sb.AppendLine($"  Loại giường: {room.BedType}");
                    sb.AppendLine($"  Mô tả phòng: {room.Description}");

                    if (room.Amenities != null && room.Amenities.Any())
                    {
                        sb.AppendLine("  Tiện nghi phòng:");

                        foreach (var amenity in room.Amenities)
                        {
                            sb.AppendLine($"  + {amenity.Name}");
                        }
                    }
                }
            }

            return sb.ToString();
        }

        private string BuildPromotionContent(Promotion p)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Mã khuyến mãi: {p.Code}");
            sb.AppendLine($"Tên khuyến mãi: {p.Title}");
            sb.AppendLine($"Mô tả: {p.Description}");
            sb.AppendLine($"Loại giảm: {(p.DiscountType == "Percent" ? "Giảm phần trăm" : "Giảm số tiền")}");
            sb.AppendLine($"Giá trị giảm: {p.DiscountValue}");
            sb.AppendLine($"Giảm tối đa: {p.MaxDiscountAmount:N0}đ");
            sb.AppendLine($"Đơn tối thiểu: {p.MinBookingAmount:N0}đ");
            sb.AppendLine($"Thời gian: {p.StartDate:dd/MM/yyyy HH:mm} đến {p.EndDate:dd/MM/yyyy HH:mm}");

            if (p.PromotionAccommodations != null && p.PromotionAccommodations.Any())
            {
                sb.AppendLine("Áp dụng cho:");
                foreach (var item in p.PromotionAccommodations)
                {
                    sb.AppendLine($"- {item.Accommodation?.Name}");
                }
            }
            else
            {
                sb.AppendLine("Áp dụng cho tất cả nơi lưu trú.");
            }

            return sb.ToString();
        }

        private List<string> SplitIntoChunks(string text, int maxLength)
        {
            var chunks = new List<string>();

            if (string.IsNullOrWhiteSpace(text))
            {
                return chunks;
            }

            var paragraphs = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            var current = new StringBuilder();

            foreach (var paragraph in paragraphs)
            {
                if (current.Length + paragraph.Length > maxLength)
                {
                    chunks.Add(current.ToString());
                    current.Clear();
                }

                current.AppendLine(paragraph);
            }

            if (current.Length > 0)
            {
                chunks.Add(current.ToString());
            }

            return chunks;
        }

        private string CreateHash(string text)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
            return Convert.ToHexString(bytes);
        }

        private int EstimateTokenCount(string text)
        {
            return Math.Max(1, text.Length / 4);
        }
    }
}