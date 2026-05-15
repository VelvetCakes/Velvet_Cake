using System;
using System.Collections.Generic;

namespace VelvetCakes.Api.Models;

public partial class Component
{
    public int Id { get; set; }

    public string Type { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal BasePricePerUnit { get; set; }

    public int? ComplexityPoints { get; set; }

    public bool? IsSeasonal { get; set; }

    public DateOnly? SeasonStart { get; set; }

    public DateOnly? SeasonEnd { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<CustomCakeComponent> CustomCakeComponents { get; set; } = new List<CustomCakeComponent>();
}
