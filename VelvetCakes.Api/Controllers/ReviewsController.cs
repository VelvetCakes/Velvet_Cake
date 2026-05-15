using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VelvetCakes.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace VelvetCakes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ReviewsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Reviews.ToListAsync());

    [HttpGet("my"), Authorize]
    public async Task<IActionResult> GetMy() => Ok(await _db.Reviews.Where(r => r.UserId == int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value)).ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReviewDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized();

            var review = new Review
            {
                UserId = userId,
                AuthorName = dto.AuthorName ?? "Аноним",
                Text = dto.Text,
                CreatedAt = DateTime.UtcNow
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();
            return Ok(review);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при создании отзыва: {ex.Message}");
            return StatusCode(500, "Произошла ошибка при сохранении отзыва.");
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
}

public class CreateReviewDto
{
    [Required(ErrorMessage = "Имя обязательно")]
    public string AuthorName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Текст отзыва обязателен")]
    public string Text { get; set; } = string.Empty;
}