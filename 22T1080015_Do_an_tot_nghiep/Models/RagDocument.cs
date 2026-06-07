namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class RagDocument
{
    public int Id { get; set; }

    public string SourceTable { get; set; } = null!;

    public int SourceId { get; set; }

    public string Title { get; set; } = null!;

    public string? ContentHash { get; set; }

    public string AiIndexStatus { get; set; } = "Indexed";

    public string? ErrorMessage { get; set; }

    public bool IsActive { get; set; }

    public DateTime? IndexedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<RagChunk> RagChunks { get; set; } = new List<RagChunk>();
}