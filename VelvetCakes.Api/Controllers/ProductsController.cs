using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VelvetCakes.Api.Models;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace VelvetCakes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    public ProductsController(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetProducts(string category = "cheesecakes")
    {
        var products = await _db.Products
            .Where(p => p.Category == category)
            .ToListAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProduct(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound("Товар не найден");
        return Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "manager")]
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name))
            return BadRequest("Название товара обязательно");

        product.CreatedAt = DateTime.UtcNow;
        _db.Products.Add(product);
        await _db.SaveChangesAsync();
        return Ok(product);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "manager")]
    public async Task<IActionResult> Update(int id, [FromBody] Product updated)
    {
        var existing = await _db.Products.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name = updated.Name;
        existing.Description = updated.Description;
        existing.Price = updated.Price;
        existing.Weight = updated.Weight;
        existing.ImageUrl = updated.ImageUrl;
        existing.Category = updated.Category;

        await _db.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "manager")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null) return NotFound();

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("upload-image")]
    [Authorize(Roles = "manager")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        try
        {
            // FIX: Проверяем, есть ли файл в запросе
            if (file == null)
            {
                Console.WriteLine("UploadImage: file is null");
                return BadRequest(new { error = "Файл не выбран" });
            }

            if (file.Length == 0)
            {
                Console.WriteLine("UploadImage: file is empty");
                return BadRequest(new { error = "Файл пуст" });
            }

            // Проверка типа файла
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { error = "Неподдерживаемый формат файла. Разрешены: JPG, PNG, GIF, WEBP" });
            }

            // Ограничение размера (5 MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { error = "Файл слишком большой. Максимальный размер: 5 MB" });
            }

            // Создаём директорию wwwroot/uploads если её нет
            var webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(webRootPath, "uploads");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
                Console.WriteLine($"Created uploads folder: {uploadsFolder}");
            }

            // Генерируем уникальное имя файла
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            Console.WriteLine($"Saving file to: {filePath}");

            // Сохраняем файл
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            Console.WriteLine($"File saved successfully: {fileName}");

            // Формируем URL
            var request = HttpContext.Request;
            var baseUrl = $"{request.Scheme}://{request.Host}";
            var imageUrl = $"{baseUrl}/uploads/{fileName}";

            return Ok(new { url = imageUrl });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return StatusCode(500, new { error = $"Ошибка сервера: {ex.Message}" });
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(Array.Empty<Product>());
        var term = q.ToLower();
        var res = await _db.Products
            .Where(p => p.Name.ToLower().Contains(term) || p.Description.ToLower().Contains(term))
            .Take(5)
            .ToListAsync();
        return Ok(res);
    }
}