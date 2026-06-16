using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VelvetCakes.Api.Models;
using System.ComponentModel.DataAnnotations;
using VelvetCakes.Api.DTOs;

namespace VelvetCakes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ReviewsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var approvedReviews = await _db.Reviews
            .Where(r => r.IsApproved == true)
            .Include(r => r.User)
            .Include(r => r.Order)
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(r => r.Order)
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.CustomCake)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return Ok(approvedReviews);
    }

    [HttpGet("pending")]
    [Authorize(Roles = "manager")]
    public async Task<IActionResult> GetPending()
    {
        var pendingReviews = await _db.Reviews
            .Where(r => r.IsApproved == false)
            .Include(r => r.User)
            .Include(r => r.Order)
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return Ok(pendingReviews);
    }

    [HttpPut("{id}/approve")]
    [Authorize(Roles = "manager")]
    public async Task<IActionResult> Approve(int id)
    {
        try
        {
            var review = await _db.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            review.IsApproved = true;
            await _db.SaveChangesAsync();

            _db.Notifications.Add(new Notification
            {
                UserId = review.UserId,
                Title = "Ваш отзыв опубликован!",
                Text = "Спасибо за ваш отзыв! Он прошел модерацию и теперь виден на сайте.",
                SentAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            return Ok(review);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Approve error: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var review = await _db.Reviews.FindAsync(id);
        if (review == null) return NotFound();

        _db.Reviews.Remove(review);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("my"), Authorize]
    public async Task<IActionResult> GetMy() =>
        Ok(await _db.Reviews.Where(r => r.UserId == int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value)).ToListAsync());

    [HttpGet("my-orders")]
    [Authorize]
    public async Task<IActionResult> GetMyOrdersForReview()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var orders = await _db.Orders
            .Where(o => o.UserId == userId && o.Status == "Доставлен")
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.CustomCake)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(orders);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            if (dto.OrderId.HasValue)
            {
                var order = await _db.Orders.FindAsync(dto.OrderId.Value);
                if (order == null || order.UserId != userId)
                    return BadRequest("Заказ не найден или не принадлежит вам");

                if (order.Status != "Доставлен")
                    return BadRequest("Отзыв можно оставить только для доставленных заказов");
            }

            var review = new Review
            {
                UserId = userId,
                OrderId = dto.OrderId,
                AuthorName = dto.AuthorName ?? "Аноним",
                Text = dto.Text,
                Rating = dto.Rating ?? 5,
                CreatedAt = DateTime.UtcNow
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();
            return Ok(review);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Произошла ошибка при сохранении отзыва.");
        }
    }
}

public class CreateReviewDto
{
    [Required(ErrorMessage = "Имя обязательно")]
    public string AuthorName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Текст отзыва обязателен")]
    public string Text { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Оценка должна быть от 1 до 5")]
    public int? Rating { get; set; } = 5;

    public int? OrderId { get; set; }
}