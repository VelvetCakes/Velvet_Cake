using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VelvetCakes.Api.Models;

namespace VelvetCakes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComponentsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ComponentsController(ApplicationDbContext db) => _db = db;

    [HttpGet("fillings")]
    public async Task<IActionResult> GetFillings() =>
        Ok(await _db.Components.Where(c => c.Type == "filling").ToListAsync());

    [HttpGet("cakeBases")]
    public async Task<IActionResult> GetCakeBases() =>
        Ok(await _db.Components.Where(c => c.Type == "cake_base").ToListAsync());

    [HttpPost("fillings")]
    [Authorize(Roles = "manager")]
    public async Task<IActionResult> AddFilling([FromBody] ComponentNameDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Название не может быть пустым");

        var component = new Component
        {
            Type = "filling",
            Name = dto.Name.Trim(),
            BasePricePerUnit = 300,
            IsSeasonal = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Components.Add(component);
        await _db.SaveChangesAsync();
        return Ok(component);
    }

    [HttpPost("cakeBases")]
    [Authorize(Roles = "manager")]
    public async Task<IActionResult> AddCakeBase([FromBody] ComponentNameDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Название не может быть пустым");

        var component = new Component
        {
            Type = "cake_base",
            Name = dto.Name.Trim(),
            BasePricePerUnit = 200,
            IsSeasonal = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Components.Add(component);
        await _db.SaveChangesAsync();
        return Ok(component);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var component = await _db.Components.FindAsync(id);
        if (component == null)
            return NotFound(new { message = "Компонент не найден" });

        // Проверяем, используется ли компонент в кастомных тортах
        var isUsed = await _db.CustomCakeComponents.AnyAsync(cc => cc.ComponentId == id);
        if (isUsed)
        {
            return BadRequest(new { message = "Невозможно удалить: компонент используется в заказах" });
        }

        _db.Components.Remove(component);
        await _db.SaveChangesAsync();
        return Ok(new { message = "Компонент удалён" });
    }

    [HttpGet]
    [Authorize(Roles = "manager")]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Components.ToListAsync());
}

public class ComponentNameDto
{
    public string Name { get; set; } = string.Empty;
}