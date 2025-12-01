using CarPartsShop.API.Data;
using CarPartsShop.API.DTOs.Parts;
using CarPartsShop.API.Models;
using CarPartsShop.API.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarPartsShop.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PartsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PartsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetParts(
            [FromQuery] string? q,
            [FromQuery] int? categoryId,
            [FromQuery] decimal? minPrice,
            [FromQuery] decimal? maxPrice,
            [FromQuery] string? sort, // name|price|newest
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            var query = _db.Parts
                .AsNoTracking()
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(term) ||
                    p.Sku.ToLower().Contains(term) ||
                    (p.Description != null && p.Description.ToLower().Contains(term)));
            }

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            query = sort?.ToLower() switch
            {
                "name" => query.OrderBy(p => p.Name),
                "price" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "newest" => query.OrderByDescending(p => p.Id),
                _ => query.OrderBy(p => p.Id)
            };

            var total = await query.CountAsync();

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PartDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Sku = p.Sku,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    Price = p.Price,
                    QuantityInStock = p.QuantityInStock,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category!.Name
                })
                .ToListAsync();

            Response.Headers["X-Total-Count"] = total.ToString();

            return Ok(new
            {
                page,
                pageSize,
                total,
                items
            });
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PartDto>> GetPartById(int id)
        {
            var part = await _db.Parts
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.Id == id)
                .Select(p => new PartDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Sku = p.Sku,
                    Description = p.Description,
                    ImageUrl = p.ImageUrl,
                    Price = p.Price,
                    QuantityInStock = p.QuantityInStock,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category!.Name
                })
                .FirstOrDefaultAsync();

            if (part == null)
                return NotFound();

            return Ok(part);
        }

        // POST: /api/parts
        [HttpPost]
        [Authorize(Roles = Roles.Administrator)]
        public async Task<IActionResult> CreatePart(CreatePartDto dto)
        {
            var part = new Part
            {
                Name = dto.Name,
                Sku = dto.Sku,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                Price = dto.Price,
                QuantityInStock = dto.QuantityInStock,
                CategoryId = dto.CategoryId
            };

            _db.Parts.Add(part);
            await _db.SaveChangesAsync();

            await LogAdminAction($"Created product {part.Name} (SKU: {part.Sku})");

            return CreatedAtAction(nameof(GetPartById), new { id = part.Id }, part);
        }
        
        [HttpPut("{id:int}")]
        [Authorize(Roles = $"{Roles.Administrator}, {Roles.SalesAssistant}")]
        public async Task<IActionResult> UpdatePart(int id, UpdatePartDto dto)
        {
            var part = await _db.Parts.FindAsync(id);
            if (part == null)
                return NotFound();

            part.Name = dto.Name;
            part.Description = dto.Description;
            part.ImageUrl = dto.ImageUrl;
            part.Price = dto.Price;
            part.QuantityInStock = dto.QuantityInStock;
            part.CategoryId = dto.CategoryId;

            await _db.SaveChangesAsync();

            await LogAdminAction($"Updated product {part.Name} (ID: {part.Id})");

            return Ok(dto);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = Roles.Administrator)]
        public async Task<IActionResult> DeletePart(int id)
        {
            var part = await _db.Parts.FindAsync(id);
            if (part == null)
                return NotFound();

            _db.Parts.Remove(part);
            await _db.SaveChangesAsync();

            await LogAdminAction($"Deleted product {part.Name} (ID: {part.Id})");

            return NoContent();
        }

        private async Task LogAdminAction(string action)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            var userEmail = User.Identity?.Name ?? "unknown";

            _db.AdminLogs.Add(new AdminLog
            {
                Timestamp = DateTime.UtcNow,
                PerformedById = userId,
                PerformedByEmail = userEmail,
                Action = action
            });

            await _db.SaveChangesAsync();
        }
    }
}
