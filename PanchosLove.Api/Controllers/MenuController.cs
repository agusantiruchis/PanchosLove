using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PanchosLove.Api.Data;
using PanchosLove.Api.Dtos;
using PanchosLove.Api.Models;

namespace PanchosLove.Api.Controllers;

/// <summary>Menú digital público (e-commerce). InStock se calcula con el inventario en tiempo real.</summary>
[ApiController]
[Route("api/menu")]
public class MenuController(AppDbContext db, IConfiguration cfg) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MenuDto>> Get()
    {
        var categories = await db.Categories.OrderBy(c => c.SortOrder).ToListAsync();
        var products = await db.Products
            .Include(p => p.Recipe).ThenInclude(r => r.Ingredient)
            .Where(p => p.IsAvailable).ToListAsync();

        static MenuItemDto Map(Product p) => new(p.Id, p.Name, p.Description, p.Price, p.ImageUrl,
            p.Recipe.All(r => r.Ingredient!.Stock >= r.Quantity));

        var cats = categories
            .Select(c => new MenuCategoryDto(c.Id, c.Name,
                products.Where(p => p.CategoryId == c.Id && !p.IsExtra).OrderBy(p => p.Name).Select(Map).ToList()))
            .Where(c => c.Items.Count > 0).ToList();
        var extras = products.Where(p => p.IsExtra).OrderBy(p => p.Name).Select(Map).ToList();

        return new MenuDto(cats, extras, cfg.GetValue<decimal>("Delivery:Fee"));
    }
}
