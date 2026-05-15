using System;
using System.Collections.Generic;

namespace VelvetCakes.Api.Models;

public partial class Chat
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? ManagerId { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual User? Manager { get; set; }
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
