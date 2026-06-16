using System;
using System.Collections.Generic;

namespace VelvetCakes.Api.Models;

public partial class Review
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? OrderId { get; set; }

    public string? AuthorName { get; set; }

    public string Text { get; set; } = null!;

    public int? Rating { get; set; }

    public bool IsApproved { get; set; } = false;

    public int? ProductId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;

    public virtual Order? Order { get; set; }

    public virtual Product? Product { get; set; }
}
