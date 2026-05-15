using System;
using System.Collections.Generic;

namespace VelvetCakes.Api.Models;

public partial class CustomCake
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Weight { get; set; }

    public decimal TotalPrice { get; set; }

    public string? DesignNotes { get; set; }

    public DateOnly DeliveryDate { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<CustomCakeComponent> CustomCakeComponents { get; set; } = new List<CustomCakeComponent>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual User? User { get; set; }
}
