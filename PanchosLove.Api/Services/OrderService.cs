using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PanchosLove.Api.Data;
using PanchosLove.Api.Dtos;
using PanchosLove.Api.Hubs;
using PanchosLove.Api.Models;

namespace PanchosLove.Api.Services;

public class OrderService(AppDbContext db, IHubContext<KitchenHub> hub, IConfiguration cfg)
{
    public async Task<OrderDto> CreateAsync(CreateOrderRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.CustomerName))
            throw new AppException(400, "Ingresá el nombre del cliente.");
        if (req.Items is null || req.Items.Count == 0)
            throw new AppException(400, "El pedido no tiene ítems.");
        if (req.Type == OrderType.Delivery && string.IsNullOrWhiteSpace(req.DeliveryAddress))
            throw new AppException(400, "El delivery requiere dirección de entrega.");

        var ids = req.Items.Select(i => i.ProductId)
            .Concat(req.Items.SelectMany(i => i.ExtraIds ?? new List<int>()))
            .Distinct().ToList();
        var products = await db.Products.Include(p => p.Recipe)
            .Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        var order = new Order
        {
            CustomerName = req.CustomerName.Trim(),
            Phone = req.Phone,
            Type = req.Type,
            DeliveryAddress = req.Type == OrderType.Delivery ? req.DeliveryAddress : null,
            PaymentMethod = req.PaymentMethod,
            Notes = req.Notes,
            DeliveryFee = req.Type == OrderType.Delivery ? cfg.GetValue<decimal>("Delivery:Fee") : 0
        };

        var needs = new Dictionary<int, decimal>(); // IngredientId -> cantidad total requerida
        decimal subtotal = 0;

        foreach (var line in req.Items)
        {
            if (line.Quantity < 1 || line.Quantity > 50)
                throw new AppException(400, "La cantidad por ítem debe estar entre 1 y 50.");
            if (!products.TryGetValue(line.ProductId, out var prod) || prod.IsExtra || !prod.IsAvailable)
                throw new AppException(400, $"El producto {line.ProductId} no está disponible.");

            var item = new OrderItem
            {
                ProductId = prod.Id, ProductName = prod.Name, UnitPrice = prod.Price,
                Quantity = line.Quantity, Notes = line.Notes
            };
            var used = new List<Product> { prod };
            decimal unitTotal = prod.Price;

            foreach (var exId in (line.ExtraIds ?? new List<int>()).Distinct())
            {
                if (!products.TryGetValue(exId, out var ex) || !ex.IsExtra || !ex.IsAvailable)
                    throw new AppException(400, $"El extra {exId} no está disponible.");
                item.Extras.Add(new OrderItemExtra { ProductId = ex.Id, Name = ex.Name, Price = ex.Price });
                used.Add(ex);
                unitTotal += ex.Price;
            }

            foreach (var p in used)
                foreach (var r in p.Recipe)
                    needs[r.IngredientId] = needs.GetValueOrDefault(r.IngredientId) + r.Quantity * line.Quantity;

            subtotal += unitTotal * line.Quantity;
            order.Items.Add(item);
        }
        order.Total = subtotal + order.DeliveryFee;

        // Control de stock y descuento de insumos
        var ingIds = needs.Keys.ToList();
        var ingredients = await db.Ingredients.Where(i => ingIds.Contains(i.Id)).ToDictionaryAsync(i => i.Id);

        var shortages = needs
            .Where(n => ingredients[n.Key].Stock < n.Value)
            .Select(n => new
            {
                insumo = ingredients[n.Key].Name,
                disponible = ingredients[n.Key].Stock,
                requerido = n.Value
            }).ToList();
        if (shortages.Count > 0)
            throw new AppException(409, "Stock insuficiente para completar el pedido.", shortages);

        foreach (var (ingId, qty) in needs)
        {
            var ing = ingredients[ingId];
            ing.Stock -= qty;
            db.StockMovements.Add(new StockMovement { Ingredient = ing, Quantity = -qty, Reason = "Venta", Order = order });
        }

        // Número correlativo diario: PL-yyMMdd-001
        var localToday = DateOnly.FromDateTime(Clock.Now);
        var dayStartUtc = Clock.ToUtc(localToday);
        var countToday = await db.Orders.CountAsync(o => o.CreatedAt >= dayStartUtc);
        order.Number = $"PL-{localToday:yyMMdd}-{countToday + 1:000}";

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var dto = OrderDto.From(order);
        await hub.Clients.All.SendAsync("OrderCreated", dto);

        var low = ingredients.Values.Where(i => i.Stock <= i.MinStock).Select(i => i.Name).ToArray();
        if (low.Length > 0) await hub.Clients.All.SendAsync("LowStock", low);

        return dto;
    }

    public async Task<OrderDto> UpdateStatusAsync(int id, OrderStatus newStatus)
    {
        var order = await db.Orders.Include(o => o.Items).ThenInclude(i => i.Extras)
            .FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new AppException(404, "Pedido no encontrado.");

        var allowed = order.Status switch
        {
            OrderStatus.Pendiente => new[] { OrderStatus.EnPreparacion, OrderStatus.Cancelado },
            OrderStatus.EnPreparacion => new[] { OrderStatus.Listo, OrderStatus.Cancelado },
            OrderStatus.Listo => new[] { OrderStatus.Entregado, OrderStatus.Cancelado },
            _ => Array.Empty<OrderStatus>()
        };
        if (!allowed.Contains(newStatus))
            throw new AppException(400, $"No se puede pasar de {order.Status} a {newStatus}.");

        if (newStatus == OrderStatus.Cancelado)
        {
            // Devuelve al stock exactamente lo que descontó la venta
            var sales = await db.StockMovements.Include(m => m.Ingredient)
                .Where(m => m.OrderId == id && m.Reason == "Venta").ToListAsync();
            foreach (var m in sales)
            {
                m.Ingredient.Stock += -m.Quantity;
                db.StockMovements.Add(new StockMovement
                {
                    IngredientId = m.IngredientId, Quantity = -m.Quantity,
                    Reason = "Anulación de pedido", OrderId = id
                });
            }
        }

        order.Status = newStatus;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var dto = OrderDto.From(order);
        await hub.Clients.All.SendAsync("OrderStatusChanged", dto);
        return dto;
    }
}
