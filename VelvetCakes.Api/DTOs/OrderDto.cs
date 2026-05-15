namespace VelvetCakes.Api.DTOs;

public class CreateOrderDto
{
    public decimal Total { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? Comments { get; set; }
    public string DeliveryDate { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public int? ProductId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal Weight { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class UpdateStatusDto
{
    public string Status { get; set; } = "Новый";
}