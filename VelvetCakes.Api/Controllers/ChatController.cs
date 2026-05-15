using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VelvetCakes.Api.Models;

namespace VelvetCakes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ChatController(ApplicationDbContext db)
    {
        _db = db;
    }

    // Получить чаты текущего пользователя (для пользователя - один чат, для менеджера - все)
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetMyChats()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var userRole = User.FindFirst(ClaimTypes.Role)!.Value;

        IQueryable<Chat> query;

        if (userRole == "manager")
        {
            query = _db.Chats
                .Include(c => c.User)
                .Include(c => c.Messages)
                .Where(c => c.Status == "active");
        }
        else
        {
            query = _db.Chats
                .Include(c => c.Messages)
                .Where(c => c.UserId == userId && c.Status == "active");
        }

        var chats = await query.ToListAsync();
        return Ok(chats);
    }

    // Создать новый чат (только для пользователя)
    [HttpPost]
    [Authorize(Roles = "user")]
    public async Task<IActionResult> CreateChat()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var existingChat = await _db.Chats
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "active");

        if (existingChat != null)
            return Ok(existingChat);

        var chat = new Chat
        {
            UserId = userId,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };

        _db.Chats.Add(chat);
        await _db.SaveChangesAsync();

        return Ok(chat);
    }

    // Получить сообщения чата
    [HttpGet("{chatId}/messages")]
    [Authorize]
    public async Task<IActionResult> GetMessages(int chatId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var userRole = User.FindFirst(ClaimTypes.Role)!.Value;

        var chat = await _db.Chats.FindAsync(chatId);
        if (chat == null) return NotFound();

        // Проверка доступа
        if (userRole != "manager" && chat.UserId != userId)
            return Forbid();

        var messages = await _db.ChatMessages
            .Where(m => m.ChatId == chatId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return Ok(messages);
    }

    // Отправить сообщение
    [HttpPost("{chatId}/messages")]
    [Authorize]
    public async Task<IActionResult> SendMessage(int chatId, [FromBody] SendMessageDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var userRole = User.FindFirst(ClaimTypes.Role)!.Value;

        var chat = await _db.Chats.FindAsync(chatId);
        if (chat == null) return NotFound();

        // Проверка доступа
        if (userRole == "manager")
        {
            if (chat.ManagerId == null)
            {
                chat.ManagerId = userId;
                chat.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }
        else if (chat.UserId != userId)
        {
            return Forbid();
        }

        var message = new ChatMessage
        {
            ChatId = chatId,
            SenderId = userId,
            SenderRole = userRole,
            Message = dto.Message,
            IsRead = false,
            SentAt = DateTime.UtcNow
        };

        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();

        return Ok(message);
    }

    // Отметить сообщения как прочитанные
    [HttpPut("{chatId}/read")]
    [Authorize]
    public async Task<IActionResult> MarkAsRead(int chatId)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var userRole = User.FindFirst(ClaimTypes.Role)!.Value;

        var chat = await _db.Chats.FindAsync(chatId);
        if (chat == null) return NotFound();

        var messages = await _db.ChatMessages
            .Where(m => m.ChatId == chatId && m.SenderId != userId && !m.IsRead)
            .ToListAsync();

        foreach (var msg in messages)
        {
            msg.IsRead = true;
        }

        await _db.SaveChangesAsync();
        return Ok();
    }
}

public class SendMessageDto
{
    public string Message { get; set; } = string.Empty;
}