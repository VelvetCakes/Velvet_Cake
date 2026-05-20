using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VelvetCakes.Api.Models;
using VelvetCakes.Api.DTOs;
using VelvetCakes.Api.Services;

namespace VelvetCakes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IYooKassaService _yooKassaService;

    public OrdersController(ApplicationDbContext db, IYooKassaService yooKassaService)
    {
        _db = db;
        _yooKassaService = yooKassaService;
    }

    [HttpPost]
    [Authorize(Roles = "user")]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var user = await _db.Users.FindAsync(userId);
        if (user == null) return Unauthorized();

        var initialStatus = dto.PaymentMethod == "online" ? "Ожидает оплаты" : "Новый";

        var order = new Order
        {
            UserId = userId,
            Status = initialStatus,
            TotalAmount = dto.Total,
            DeliveryAddress = dto.DeliveryAddress,
            Comments = dto.Comments,
            DesiredDeliveryDate = DateOnly.Parse(dto.DeliveryDate),
            PaymentMethod = dto.PaymentMethod ?? "Карта при получении",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        foreach (var item in dto.Items)
        {
            OrderItem orderItem;

            if (item.ProductId.HasValue && !item.IsCustom)
            {
                orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price
                };
            }
            else
            {
                var customCake = new CustomCake
                {
                    UserId = userId,
                    Name = item.Name ?? "Индивидуальный торт",
                    Description = item.Description ?? item.CustomData?.DesignNotes,
                    Weight = item.Weight,
                    TotalPrice = item.Price,
                    DeliveryDate = DateOnly.Parse(dto.DeliveryDate),
                    CreatedAt = DateTime.UtcNow
                };
                _db.CustomCakes.Add(customCake);
                await _db.SaveChangesAsync();

                orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    CustomCakeId = customCake.Id,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price
                };
            }
            _db.OrderItems.Add(orderItem);
        }
        await _db.SaveChangesAsync();

        if (dto.PaymentMethod == "online")
        {
            return Ok(new { order, requiresPayment = true });
        }

        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = $"Заказ №{order.Id} принят",
            Text = $"Ваш заказ на сумму {dto.Total} ₽ принят в работу. Статус: \"Новый\".",
            SentAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return Ok(order);
    }

    [HttpGet]
    [Authorize(Roles = "manager,pastry_chef")]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _db.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.CustomCake)
            .ToListAsync();
        return Ok(orders);
    }

    [HttpGet("my"), Authorize]
    public async Task<IActionResult> GetMy()
    {
        var uid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return Ok(await _db.Orders.Where(o => o.UserId == uid).Include(o => o.OrderItems).ThenInclude(i => i.Product).ToListAsync());
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "manager,pastry_chef")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
    {
        var order = await _db.Orders
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        var oldStatus = order.Status;

        if (oldStatus != dto.Status)
        {
            order.Status = dto.Status;
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _db.Notifications.Add(new Notification
            {
                UserId = order.UserId,
                Title = $"Статус заказа №{id} обновлён",
                Text = $"Статус изменён с \"{oldStatus}\" на \"{dto.Status}\".",
                SentAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        return Ok(order);
    }

    [HttpPost("{id}/payment")]
    [Authorize(Roles = "user")]
    public async Task<IActionResult> CreatePayment(int id, [FromBody] PaymentRequestDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var order = await _db.Orders.FindAsync(id);

        if (order == null)
            return NotFound(new { error = "Заказ не найден" });

        if (order.UserId != userId)
            return Forbid();

        if (order.PaidAmount > 0)
            return BadRequest(new { error = "Заказ уже оплачен" });

        if (order.Status != "Ожидает оплаты")
            return BadRequest(new { error = "Заказ не может быть оплачен. Текущий статус: " + order.Status });

        var paymentResponse = await _yooKassaService.CreatePaymentAsync(
            order.TotalAmount,
            $"Заказ #{order.Id} в Velvet",
            dto.ReturnUrl ?? $"{_config["FrontendUrl"]}/payment.html?orderId={order.Id}",
            "embedded" 
        );

        if (paymentResponse == null || string.IsNullOrEmpty(paymentResponse.Confirmation?.ConfirmationToken))
        {
            _logger.LogError($"Failed to create payment for order {id}");
            return StatusCode(500, new { error = "Ошибка создания платежа в ЮKassa" });
        }

        return Ok(new
        {
            confirmationToken = paymentResponse.Confirmation.ConfirmationToken,
            paymentId = paymentResponse.Id,
            status = paymentResponse.Status
        });
    }

    public class PaymentRequestDto
    {
        public string ReturnUrl { get; set; } = string.Empty;
    }
}