namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class BotQuestionLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Question { get; set; } = null!;

    public string NormalizedQuestion { get; set; } = null!;

    public string? Answer { get; set; }

    public string? Intent { get; set; }

    public bool IsInScope { get; set; }

    public DateTime CreatedAt { get; set; }
}