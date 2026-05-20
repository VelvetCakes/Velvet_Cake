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
    public bool IsCustom { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal Weight { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public CustomCakeDataDto? CustomData { get; set; }
}

public class CustomCakeDataDto
{
    public decimal Weight { get; set; }
    public List<string> Fillings { get; set; } = new();
    public List<string> CakeBases { get; set; } = new();
    public string? DesignNotes { get; set; }
    public string? DeliveryDate { get; set; }
}

public class UpdateStatusDto
{
    public string Status { get; set; } = "Новый";
}