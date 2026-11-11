using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PanchosLove.Api.Data;
using PanchosLove.Api.Dtos;
using PanchosLove.Api.Models;
using PanchosLove.Api.Services;

namespace PanchosLove.Api.Controllers;

/// <summary>Panel de cocina: cola de pedidos activos, del más antiguo al más nuevo.</summary>
[ApiController]
[Authorize(Roles = $"{Roles.Admin},{Roles.Cocina},{Roles.Cajero}")]
[Route("api/kitchen")]
public class KitchenController(AppDbContext db, OrderService orders) : ControllerBase
{
    [HttpGet("orders")]
    public async Task<IEnumerable<OrderDto>> Active()
    {
        var list = await db.Orders.Include(o => o.Items).ThenInclude(i => i.Extras)
            .Where(o => o.Status == OrderStatus.Pendiente
                     || o.Status == OrderStatus.EnPreparacion
                     || o.Status == OrderStatus.Listo)
            .OrderBy(o => o.CreatedAt).ToListAsync();
        return list.Select(OrderDto.From);
    }

    /// <summary>Cocina avanza el pedido: Pendiente → EnPreparacion → Listo.</summary>
    [HttpPut("orders/{id:int}/status")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(int id, UpdateStatusRequest req)
    {
        var isCocinaOnly = User.IsInRole(Roles.Cocina) && !User.IsInRole(Roles.Admin) && !User.IsInRole(Roles.Cajero);
        if (isCocinaOnly && (req.Status == OrderStatus.Entregado || req.Status == OrderStatus.Cancelado))
            return Forbid();
        return await orders.UpdateStatusAsync(id, req.Status);
    }
}
