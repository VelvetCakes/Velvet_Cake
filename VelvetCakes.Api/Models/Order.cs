using System;
using System.Collections.Generic;

namespace VelvetCakes.Api.Models;

public partial class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Status { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public decimal? PaidAmount { get; set; }

    public string? DeliveryAddress { get; set; }

    public string? Comments { get; set; }

    public DateOnly DesiredDeliveryDate { get; set; }

    public string? PaymentMethod { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual User User { get; set; } = null!;
}
