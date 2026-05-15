using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VelvetCakes.Api.Models;

namespace VelvetCakes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public NotificationsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetUserNotifications()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var notifications = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.SentAt)
            .ToListAsync();

        return Ok(notifications);
    }

    [HttpPost]
    [Authorize(Roles = "manager")]
    public async Task<IActionResult> Send([FromBody] SendNotificationDto dto)
    {
        var notification = new Notification
        {
            UserId = dto.UserId,
            Title = dto.Title,
            Text = dto.Text,
            SentAt = DateTime.UtcNow
        };
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
        return Ok(notification);
    }

    [HttpDelete("clear")]
    [Authorize]
    public async Task<IActionResult> Clear()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var userNotifications = await _db.Notifications
            .Where(n => n.UserId == userId)
            .ToListAsync();

        _db.Notifications.RemoveRange(userNotifications);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("send-by-email")]
    [Authorize(Roles = "manager")]
    public async Task<IActionResult> SendByEmail([FromBody] SendByEmailDto dto)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null) return BadRequest("Пользователь не найден");

        _db.Notifications.Add(new Notification
        {
            UserId = user.Id,
            Title = dto.Title,
            Text = dto.Text,
            SentAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return Ok();
    }

    public class SendByEmailDto
    {
        public string Email { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}

public class SendNotificationDto
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}