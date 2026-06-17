using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VelvetCakes.Api.Models;

namespace VelvetCakes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhooksController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(ApplicationDbContext db, ILogger<WebhooksController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPost("yookassa")]
    public async Task<IActionResult> HandleYooKassaWebhook()
    {
        try
        {
            var body = await new StreamReader(Request.Body).ReadToEndAsync();
            _logger.LogInformation($"YooKassa webhook received: {body}");

            var json = JsonDocument.Parse(body);
            var root = json.RootElement;

            if (root.TryGetProperty("event", out var eventElement) &&
                eventElement.GetString() == "payment.succeeded")
            {
                if (root.TryGetProperty("object", out var paymentObject) &&
                    paymentObject.TryGetProperty("metadata", out var metadata) &&
                    metadata.TryGetProperty("orderId", out var orderIdElement))
                {
                    var orderId = int.Parse(orderIdElement.GetString()!);
                    var order = await _db.Orders.FindAsync(orderId);

                    if (order != null && order.Status == "Ожидает оплаты")
                    {
                        order.Status = "Новый";
                        order.PaidAmount = order.TotalAmount;
                        order.UpdatedAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync();

                        _db.Notifications.Add(new Notification
                        {
                            UserId = order.UserId,
                            Title = $"Заказ #{order.Id} оплачен!",
                            Text = $"Ваш заказ на сумму {order.TotalAmount} ₽ успешно оплачен. Статус заказа: \"Новый\".",
                            SentAt = DateTime.UtcNow
                        });
                        await _db.SaveChangesAsync();

                        _logger.LogInformation($"Order {orderId} status updated to 'Новый' after payment");
                        return Ok();
                    }
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Webhook error: {ex.Message}");
            return StatusCode(500);
        }
    }
}