using System;
using System.Collections.Generic;

namespace VelvetCakes.Api.Models;

public partial class UserPaymentMethod
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string MethodName { get; set; } = null!;

    public string? CardLast4 { get; set; }

    public bool? IsDefault { get; set; }

    public virtual User User { get; set; } = null!;
}
