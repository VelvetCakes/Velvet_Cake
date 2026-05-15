using System;
using System.Collections.Generic;

namespace VelvetCakes.Api.Models;

public partial class Cart
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? ProductId { get; set; }

    public int? CustomCakeId { get; set; }

    public int Quantity { get; set; }

    public DateTime? AddedAt { get; set; }

    public virtual CustomCake? CustomCake { get; set; }

    public virtual Product? Product { get; set; }

    public virtual User User { get; set; } = null!;
}
