using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PanchosLove.Api.Hubs;

/// <summary>
/// Canal en tiempo real para el panel de cocina / caja.
/// Eventos: OrderCreated, OrderStatusChanged (OrderDto) y LowStock (string[] de insumos).
/// </summary>
[Authorize]
public class KitchenHub : Hub { }
