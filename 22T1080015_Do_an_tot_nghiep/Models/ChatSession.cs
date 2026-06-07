using System;
using System.Collections.Generic;

namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class ChatSession
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? SessionTitle { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? LastUpdatedAt { get; set; }

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual User User { get; set; } = null!;
}
