using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PanchosLove.Api.Data;
using PanchosLove.Api.Dtos;
using PanchosLove.Api.Models;
using PanchosLove.Api.Services;

namespace PanchosLove.Api.Controllers;

/// <summary>Inventario de insumos (panes, salchichas, aderezos, toppings, bebidas).</summary>
[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/ingredients")]
public class IngredientsController(AppDbContext db) : ControllerBase
{
    private static IngredientDto ToDto(Ingredient i) =>
        new(i.Id, i.Name, i.Unit, i.Stock, i.MinStock, i.CostPerUnit, i.Stock <= i.MinStock);

    [HttpGet]
    public async Task<IEnumerable<IngredientDto>> List([FromQuery] bool lowStock = false)
    {
        // Se filtra en memoria: SQLite no compara decimales en SQL.
        var all = (await db.Ingredients.OrderBy(i => i.Name).ToListAsync()).Select(ToDto);
        return lowStock ? all.Where(i => i.LowStock) : all;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IngredientDto>> Get(int id)
    {
        var i = await db.Ingredients.FindAsync(id);
        return i is null ? NotFound() : ToDto(i);
    }

    [HttpPost]
    public async Task<ActionResult<IngredientDto>> Create(IngredientRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) throw new AppException(400, "El nombre es obligatorio.");
        if (req.Stock < 0 || req.MinStock < 0) throw new AppException(400, "Los valores no pueden ser negativos.");
        var i = new Ingredient
        {
            Name = req.Name.Trim(), Unit = req.Unit, Stock = req.Stock,
            MinStock = req.MinStock, CostPerUnit = req.CostPerUnit
        };
        db.Ingredients.Add(i);
        if (req.Stock > 0)
            db.StockMovements.Add(new StockMovement { Ingredient = i, Quantity = req.Stock, Reason = "Stock inicial" });
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = i.Id }, ToDto(i));
    }

    /// <summary>Edita datos del insumo. El stock se modifica solo con /restock o /adjust para dejar trazabilidad.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<IngredientDto>> Update(int id, IngredientRequest req)
    {
        var i = await db.Ingredients.FindAsync(id);
        if (i is null) return NotFound();
        i.Name = req.Name.Trim(); i.Unit = req.Unit; i.MinStock = req.MinStock; i.CostPerUnit = req.CostPerUnit;
        await db.SaveChangesAsync();
        return ToDto(i);
    }

    /// <summary>Registra una compra / ingreso de mercadería.</summary>
    [HttpPost("{id:int}/restock")]
    public async Task<ActionResult<IngredientDto>> Restock(int id, RestockRequest req)
    {
        if (req.Quantity <= 0) throw new AppException(400, "La cantidad debe ser mayor a 0.");
        var i = await db.Ingredients.FindAsync(id);
        if (i is null) return NotFound();
        i.Stock += req.Quantity;
        db.StockMovements.Add(new StockMovement { IngredientId = id, Quantity = req.Quantity, Reason = req.Reason ?? "Compra" });
        await db.SaveChangesAsync();
        return ToDto(i);
    }

    /// <summary>Ajuste por inventario físico (conteo real).</summary>
    [HttpPost("{id:int}/adjust")]
    public async Task<ActionResult<IngredientDto>> Adjust(int id, AdjustStockRequest req)
    {
        if (req.NewStock < 0) throw new AppException(400, "El stock no puede ser negativo.");
        var i = await db.Ingredients.FindAsync(id);
        if (i is null) return NotFound();
        var diff = req.NewStock - i.Stock;
        i.Stock = req.NewStock;
        db.StockMovements.Add(new StockMovement { IngredientId = id, Quantity = diff, Reason = req.Reason ?? "Ajuste de inventario" });
        await db.SaveChangesAsync();
        return ToDto(i);
    }

    [HttpGet("{id:int}/movements")]
    public async Task<IEnumerable<StockMovementDto>> Movements(int id, [FromQuery] int take = 100)
    {
        var list = await db.StockMovements.Where(m => m.IngredientId == id)
            .OrderByDescending(m => m.CreatedAt).Take(Math.Clamp(take, 1, 500)).ToListAsync();
        return list.Select(m => new StockMovementDto(m.Id, m.Quantity, m.Reason, m.OrderId, m.CreatedAt));
    }
}
