using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PanchosLove.Api.Data;
using PanchosLove.Api.Dtos;
using PanchosLove.Api.Models;
using PanchosLove.Api.Services;

namespace PanchosLove.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api")]
public class ProductsController(AppDbContext db) : ControllerBase
{
    // ---------- Categorías ----------
    [HttpGet("categories")]
    public async Task<IEnumerable<object>> Categories() =>
        await db.Categories.OrderBy(c => c.SortOrder)
            .Select(c => (object)new { c.Id, c.Name, c.SortOrder }).ToListAsync();

    [HttpPost("categories")]
    public async Task<IActionResult> CreateCategory(CategoryRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) throw new AppException(400, "El nombre es obligatorio.");
        var c = new Category { Name = req.Name.Trim(), SortOrder = req.SortOrder };
        db.Categories.Add(c);
        await db.SaveChangesAsync();
        return Created($"api/categories/{c.Id}", new { c.Id, c.Name, c.SortOrder });
    }

    [HttpPut("categories/{id:int}")]
    public async Task<IActionResult> UpdateCategory(int id, CategoryRequest req)
    {
        var c = await db.Categories.FindAsync(id);
        if (c is null) return NotFound();
        c.Name = req.Name.Trim(); c.SortOrder = req.SortOrder;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---------- Productos ----------
    [HttpGet("products")]
    public async Task<IEnumerable<ProductDto>> List()
    {
        var list = await Query().OrderBy(p => p.CategoryId).ThenBy(p => p.Name).ToListAsync();
        return list.Select(ToDto);
    }

    [HttpGet("products/{id:int}")]
    public async Task<ActionResult<ProductDto>> Get(int id)
    {
        var p = await Query().FirstOrDefaultAsync(p => p.Id == id);
        return p is null ? NotFound() : ToDto(p);
    }

    [HttpPost("products")]
    public async Task<ActionResult<ProductDto>> Create(ProductRequest req)
    {
        await Validate(req);
        var p = new Product();
        Apply(p, req);
        db.Products.Add(p);
        await db.SaveChangesAsync();
        var created = await Query().FirstAsync(x => x.Id == p.Id);
        return CreatedAtAction(nameof(Get), new { id = p.Id }, ToDto(created));
    }

    [HttpPut("products/{id:int}")]
    public async Task<ActionResult<ProductDto>> Update(int id, ProductRequest req)
    {
        await Validate(req);
        var p = await db.Products.Include(x => x.Recipe).FirstOrDefaultAsync(x => x.Id == id);
        if (p is null) return NotFound();
        db.RecipeItems.RemoveRange(p.Recipe);
        p.Recipe.Clear();
        Apply(p, req);
        await db.SaveChangesAsync();
        return ToDto(await Query().FirstAsync(x => x.Id == id));
    }

    /// <summary>Pausa o reactiva un producto en el menú sin borrarlo.</summary>
    [HttpPatch("products/{id:int}/availability")]
    public async Task<IActionResult> SetAvailability(int id, [FromQuery] bool available)
    {
        var p = await db.Products.FindAsync(id);
        if (p is null) return NotFound();
        p.IsAvailable = available;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("products/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await db.Products.FindAsync(id);
        if (p is null) return NotFound();
        db.Products.Remove(p);
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ---------- helpers ----------
    private IQueryable<Product> Query() => db.Products.Include(p => p.Category)
        .Include(p => p.Recipe).ThenInclude(r => r.Ingredient);

    private async Task Validate(ProductRequest req)
    {
        var recipe = req.Recipe ?? new List<RecipeItemRequest>();
        if (string.IsNullOrWhiteSpace(req.Name)) throw new AppException(400, "El nombre es obligatorio.");
        if (req.Price < 0) throw new AppException(400, "El precio no puede ser negativo.");
        if (!await db.Categories.AnyAsync(c => c.Id == req.CategoryId))
            throw new AppException(400, "La categoría no existe.");
        var ingIds = recipe.Select(r => r.IngredientId).ToList();
        if (ingIds.Distinct().Count() != ingIds.Count)
            throw new AppException(400, "La receta tiene insumos repetidos.");
        if (recipe.Any(r => r.Quantity <= 0))
            throw new AppException(400, "Las cantidades de la receta deben ser mayores a 0.");
        var found = await db.Ingredients.CountAsync(i => ingIds.Contains(i.Id));
        if (found != ingIds.Count) throw new AppException(400, "Algún insumo de la receta no existe.");
    }

    private static void Apply(Product p, ProductRequest req)
    {
        p.Name = req.Name.Trim(); p.Description = req.Description; p.Price = req.Price;
        p.ImageUrl = req.ImageUrl; p.IsAvailable = req.IsAvailable; p.IsExtra = req.IsExtra;
        p.CategoryId = req.CategoryId;
        foreach (var r in req.Recipe ?? new List<RecipeItemRequest>())
            p.Recipe.Add(new RecipeItem { IngredientId = r.IngredientId, Quantity = r.Quantity });
    }

    private static ProductDto ToDto(Product p) => new(p.Id, p.Name, p.Description, p.Price, p.ImageUrl,
        p.IsAvailable, p.IsExtra, p.CategoryId, p.Category?.Name,
        p.Recipe.Select(r => new RecipeItemDto(r.IngredientId, r.Ingredient!.Name, r.Ingredient.Unit, r.Quantity)).ToList());
}
