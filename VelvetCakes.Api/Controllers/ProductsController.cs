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
        try
        {
            var product = await _db.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { error = "Товар не найден" });

            return Ok(product);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting product: {ex.Message}");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "manager")]
    public async Task<IActionResult> Create([FromBody] Product product)
    {
        try
        {
            Console.WriteLine($"=== CREATE PRODUCT ===");
            Console.WriteLine($"Name: {product.Name}, Price: {product.Price}");
            Console.WriteLine($"ImageBase64 length: {product.ImageBase64?.Length ?? 0}");

            if (string.IsNullOrWhiteSpace(product.Name))
                return BadRequest(new { error = "Название товара обязательно" });

            product.CreatedAt = DateTime.UtcNow;

            if (product.ImageBase64 != null && product.ImageBase64.Length > 1000000)
            {
                Console.WriteLine($"Warning: ImageBase64 is large ({product.ImageBase64.Length} chars)");
            }

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            Console.WriteLine($"Product created with ID: {product.Id}");
            return Ok(product);
        }
        catch (DbUpdateException dbEx)
        {
            Console.WriteLine($"DB Error: {dbEx.InnerException?.Message ?? dbEx.Message}");
            return StatusCode(500, new { error = $"Ошибка базы данных: {dbEx.InnerException?.Message ?? dbEx.Message}" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating product: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return StatusCode(500, new { error = $"Ошибка: {ex.Message}" });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "manager")]
    public async Task<IActionResult> Update(int id, [FromBody] Product updated)
    {
        try
        {
            Console.WriteLine($"=== UPDATE PRODUCT {id} ===");
            Console.WriteLine($"Received data: Name={updated?.Name}, Price={updated?.Price}");
            Console.WriteLine($"ImageBase64 length: {updated?.ImageBase64?.Length ?? 0}");

            var existing = await _db.Products.FindAsync(id);
            if (existing == null)
            {
                Console.WriteLine($"Product {id} not found");
                return NotFound(new { error = "Товар не найден" });
            }

            existing.Name = updated.Name ?? existing.Name;
            existing.Description = updated.Description ?? existing.Description;
            existing.Price = updated.Price;
            existing.Weight = updated.Weight ?? existing.Weight;
            existing.Category = updated.Category ?? existing.Category;

            if (!string.IsNullOrEmpty(updated.ImageBase64))
            {
                existing.ImageBase64 = updated.ImageBase64;
                existing.ImageUrl = null;
                Console.WriteLine($"ImageBase64 saved, length: {existing.ImageBase64.Length}");
            }
            else if (!string.IsNullOrEmpty(updated.ImageUrl))
            {
                existing.ImageUrl = updated.ImageUrl;
                existing.ImageBase64 = null;
                Console.WriteLine($"ImageUrl saved: {existing.ImageUrl}");
            }

            await _db.SaveChangesAsync();
            Console.WriteLine($"Product {id} updated successfully");

            return Ok(existing);
        }
        catch (DbUpdateException dbEx)
        {
            Console.WriteLine($"DB Error: {dbEx.InnerException?.Message ?? dbEx.Message}");
            return StatusCode(500, new { error = $"Ошибка базы данных: {dbEx.InnerException?.Message ?? dbEx.Message}" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating product: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return StatusCode(500, new { error = $"Ошибка: {ex.Message}" });
        }
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

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            if (!allowedExtensions.Contains(extension))
                return BadRequest(new { error = "Неподдерживаемый формат файла. Разрешены: JPG, PNG, GIF, WEBP" });

            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { error = "Файл слишком большой. Максимальный размер: 5 MB" });

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();
            var base64String = Convert.ToBase64String(imageBytes);

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

            var productsWithStats = await _db.Products
                .Select(p => new
                {
                    Product = p,
                    OrderCount = _db.OrderItems.Where(oi => oi.ProductId == p.Id).Sum(oi => (int?)oi.Quantity) ?? 0,
                    AvgRating = _db.Reviews.Where(r => r.ProductId == p.Id && r.IsApproved).Average(r => (double?)r.Rating) ?? 0,
                    ReviewCount = _db.Reviews.Count(r => r.ProductId == p.Id && r.IsApproved)
                })
                .ToListAsync();

            var popularProducts = productsWithStats
                .Select(x => new
                {
                    x.Product,
                    PopularityScore = (x.OrderCount * 0.6) + (x.AvgRating * 10 * 0.3) + (Math.Min(x.ReviewCount, 20) * 0.1)
                })
                .OrderByDescending(x => x.PopularityScore)
                .Take(limit)
                .Select(x => x.Product)
                .ToList();

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