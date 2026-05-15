using System;
using System.Collections.Generic;

namespace VelvetCakes.Api.Models;

public partial class ChatMessage
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public int SenderId { get; set; }
    public string SenderRole { get; set; } = null!;
    public string Message { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }

    public virtual Chat Chat { get; set; } = null!;
    public virtual User Sender { get; set; } = null!;
}
