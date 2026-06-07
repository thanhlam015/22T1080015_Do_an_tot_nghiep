using System;
using System.Collections.Generic;

namespace _22T1080015_Do_an_tot_nghiep.Models;

public partial class ChatMessage
{
    public int Id { get; set; }

    public int SessionId { get; set; }

    public string SenderRole { get; set; } = null!;

    public string MessageContent { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public virtual ChatSession Session { get; set; } = null!;
}
