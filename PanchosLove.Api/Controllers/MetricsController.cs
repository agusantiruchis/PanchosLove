using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PanchosLove.Api.Data;
using PanchosLove.Api.Models;
using PanchosLove.Api.Services;

namespace PanchosLove.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.Admin)]
[Route("api/metrics")]
public class MetricsController(AppDbContext db) : ControllerBase
{
    /// <summary>Resumen de ventas. Por defecto, últimos 30 días (hora Argentina).</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var today = DateOnly.FromDateTime(Clock.Now);
        var f = from ?? today.AddDays(-29);
        var t = to ?? today;
        if (t < f) return BadRequest(new { error = "'to' no puede ser anterior a 'from'." });

        var fromUtc = Clock.ToUtc(f);
        var toUtc = Clock.ToUtc(t.AddDays(1));

        // Agregamos en memoria: SQLite no soporta SUM/ORDER BY sobre decimal.
        var all = await db.Orders.Include(o => o.Items)
            .Where(o => o.CreatedAt >= fromUtc && o.CreatedAt < toUtc).ToListAsync();
        var valid = all.Where(o => o.Status != OrderStatus.Cancelado).ToList();
        var total = valid.Sum(o => o.Total);
        var lowStock = (await db.Ingredients.ToListAsync()).Count(i => i.Stock <= i.MinStock);

        return Ok(new
        {
            from = f, to = t,
            totalSales = total,
            orders = valid.Count,
            cancelledOrders = all.Count - valid.Count,
            averageTicket = valid.Count == 0 ? 0 : Math.Round(total / valid.Count, 2),
            lowStockIngredients = lowStock,
            byType = valid.GroupBy(o => o.Type)
                .Select(g => new { type = g.Key.ToString(), orders = g.Count(), sales = g.Sum(o => o.Total) }),
            byPaymentMethod = valid.GroupBy(o => o.PaymentMethod)
                .Select(g => new { method = g.Key.ToString(), orders = g.Count(), sales = g.Sum(o => o.Total) }),
            salesByDay = valid.GroupBy(o => Clock.ToLocal(o.CreatedAt).Date).OrderBy(g => g.Key)
                .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), orders = g.Count(), sales = g.Sum(o => o.Total) }),
            salesByHour = valid.GroupBy(o => Clock.ToLocal(o.CreatedAt).Hour).OrderBy(g => g.Key)
                .Select(g => new { hour = g.Key, orders = g.Count(), sales = g.Sum(o => o.Total) }),
            topProducts = valid.SelectMany(o => o.Items).GroupBy(i => i.ProductName)
                .Select(g => new { product = g.Key, quantity = g.Sum(i => i.Quantity), revenue = g.Sum(i => i.UnitPrice * i.Quantity) })
                .OrderByDescending(x => x.quantity).Take(10)
        });
    }
}
