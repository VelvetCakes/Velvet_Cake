using System;
using System.Collections.Generic;

namespace VelvetCakes.Api.Models;

public partial class CustomCakeComponent
{
    public int Id { get; set; }

    public int CustomCakeId { get; set; }

    public int ComponentId { get; set; }

    public int Quantity { get; set; }

    public virtual Component Component { get; set; } = null!;

    public virtual CustomCake CustomCake { get; set; } = null!;
}
