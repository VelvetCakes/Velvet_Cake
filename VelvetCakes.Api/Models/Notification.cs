using System;
using System.Collections.Generic;

namespace VelvetCakes.Api.Models;

public partial class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = null!;

    public string Text { get; set; } = null!;

    public bool? IsRead { get; set; }

    public DateTime? SentAt { get; set; }

    public virtual User User { get; set; } = null!;
}
