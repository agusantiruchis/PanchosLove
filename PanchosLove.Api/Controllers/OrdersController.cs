using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PanchosLove.Api.Data;
using PanchosLove.Api.Dtos;
using PanchosLove.Api.Models;
using PanchosLove.Api.Services;

namespace PanchosLove.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(AppDbContext db, OrderService orders) : ControllerBase
{
    /// <summary>Crea un pedido (web o mostrador). Descuenta stock y avisa a cocina en tiempo real.</summary>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("orders")]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest req)
    {
        var dto = await orders.CreateAsync(req);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    /// <summary>Seguimiento público del estado por número de pedido.</summary>
    [HttpGet("track/{number}")]
    [AllowAnonymous]
    public async Task<ActionResult<TrackingDto>> Track(string number)
    {
        var o = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Number == number);
        return o is null ? NotFound() : new TrackingDto(o.Number, o.Status, o.Type, o.CreatedAt, o.UpdatedAt);
    }

    [HttpGet]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Cajero}")]
    public async Task<IEnumerable<OrderDto>> List([FromQuery] OrderStatus? status, [FromQuery] DateOnly? date)
    {
        var day = date ?? DateOnly.FromDateTime(Clock.Now);
        var from = Clock.ToUtc(day);
        var to = Clock.ToUtc(day.AddDays(1));
        var q = db.Orders.Include(o => o.Items).ThenInclude(i => i.Extras)
            .Where(o => o.CreatedAt >= from && o.CreatedAt < to);
        if (status is not null) q = q.Where(o => o.Status == status);
        var list = await q.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return list.Select(OrderDto.From);
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Cajero},{Roles.Cocina}")]
    public async Task<ActionResult<OrderDto>> GetById(int id)
    {
        var o = await db.Orders.Include(o => o.Items).ThenInclude(i => i.Extras).FirstOrDefaultAsync(o => o.Id == id);
        return o is null ? NotFound() : OrderDto.From(o);
    }

    /// <summary>Cambia el estado (incluye anulación con reposición de stock).</summary>
    [HttpPut("{id:int}/status")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Cajero}")]
    public async Task<ActionResult<OrderDto>> UpdateStatus(int id, UpdateStatusRequest req) =>
        await orders.UpdateStatusAsync(id, req.Status);
}
