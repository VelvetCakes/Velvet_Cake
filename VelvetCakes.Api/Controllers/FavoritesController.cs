using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VelvetCakes.Api.Models;
using System.Security.Claims;

namespace VelvetCakes.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FavoritesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        public FavoritesController(ApplicationDbContext db) => _db = db;

        [HttpPost, Authorize]
        public async Task<IActionResult> Add([FromBody] int productId)
        {
            var uid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            if (await _db.Favorites.AnyAsync(f => f.UserId == uid && f.ProductId == productId)) return BadRequest("Уже в избранном");
            _db.Favorites.Add(new Favorite { UserId = uid, ProductId = productId, AddedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync(); return Ok();
        }

        [HttpDelete("{id}"), Authorize]
        public async Task<IActionResult> Remove(int id)
        {
            var uid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var f = await _db.Favorites.FirstOrDefaultAsync(x => x.Id == id && x.UserId == uid);
            if (f == null) return NotFound();
            _db.Favorites.Remove(f); await _db.SaveChangesAsync(); return Ok();
        }

        [HttpGet, Authorize]
        public async Task<IActionResult> GetMy()
        {
            var uid = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await _db.Favorites.Where(f => f.UserId == uid).Include(f => f.Product).ToListAsync());
        }
    }
}
