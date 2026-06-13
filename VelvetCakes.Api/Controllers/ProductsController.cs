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
        existing.ImageBase64 = updated.ImageBase64;  // Сохраняем Base64 изображение
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
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Файл не выбран" });

            // Проверка типа файла
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { error = "Неподдерживаемый формат файла. Разрешены: JPG, PNG, GIF, WEBP" });

            // Ограничение размера (5 MB)
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { error = "Файл слишком большой. Максимальный размер: 5 MB" });

            // Конвертируем изображение в Base64
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();
            var base64String = Convert.ToBase64String(imageBytes);

            // Определяем MIME тип
            var mimeType = file.ContentType;
            var dataUrl = $"data:{mimeType};base64,{base64String}";

            return Ok(new { url = dataUrl, base64 = base64String, mimeType = mimeType });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
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

    [HttpGet("popular")]
    public async Task<IActionResult> GetPopularProducts(int limit = 1)
    {
        try
        {
            var popularProducts = await _db.OrderItems
                .Where(oi => oi.ProductId != null)
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    OrderCount = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(x => x.OrderCount)
                .Take(limit)
                .Join(_db.Products,
                      pop => pop.ProductId,
                      product => product.Id,
                      (pop, product) => product)
                .ToListAsync();

            if (popularProducts == null || popularProducts.Count == 0)
            {
                popularProducts = await _db.Products
                    .OrderBy(x => Guid.NewGuid())
                    .Take(limit)
                    .ToListAsync();
            }

            return Ok(popularProducts);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting popular products: {ex.Message}");
            var randomProducts = await _db.Products
                .OrderBy(x => Guid.NewGuid())
                .Take(limit)
                .ToListAsync();
            return Ok(randomProducts);
        }
    }
}