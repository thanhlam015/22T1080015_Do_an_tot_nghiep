namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class RagChunk
{
    public int Id { get; set; }

    public int DocumentId { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = null!;

    public string? EmbeddingJson { get; set; }

    public string? EmbeddingModel { get; set; }

    public int? TokenCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual RagDocument Document { get; set; } = null!;
}