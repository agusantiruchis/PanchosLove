using PanchosLove.Api.Models;

namespace PanchosLove.Api.Dtos;

// ---- Auth ----
public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, DateTime ExpiresAt, string Name, string Role);

// ---- Menú público ----
public record MenuItemDto(int Id, string Name, string? Description, decimal Price, string? ImageUrl, bool InStock);
public record MenuCategoryDto(int Id, string Name, List<MenuItemDto> Items);
public record MenuDto(List<MenuCategoryDto> Categories, List<MenuItemDto> Extras, decimal DeliveryFee);

// ---- Productos / Categorías (admin) ----
public record RecipeItemRequest(int IngredientId, decimal Quantity);
public record RecipeItemDto(int IngredientId, string IngredientName, string Unit, decimal Quantity);
public record ProductRequest(string Name, string? Description, decimal Price, string? ImageUrl,
    bool IsAvailable, bool IsExtra, int CategoryId, List<RecipeItemRequest>? Recipe);
public record ProductDto(int Id, string Name, string? Description, decimal Price, string? ImageUrl,
    bool IsAvailable, bool IsExtra, int CategoryId, string? CategoryName, List<RecipeItemDto> Recipe);
public record CategoryRequest(string Name, int SortOrder);

// ---- Inventario ----
public record IngredientRequest(string Name, string Unit, decimal Stock, decimal MinStock, decimal CostPerUnit);
public record IngredientDto(int Id, string Name, string Unit, decimal Stock, decimal MinStock, decimal CostPerUnit, bool LowStock);
public record RestockRequest(decimal Quantity, string? Reason);
public record AdjustStockRequest(decimal NewStock, string? Reason);
public record StockMovementDto(int Id, decimal Quantity, string Reason, int? OrderId, DateTime CreatedAt);

// ---- Pedidos ----
public class OrderLineRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public List<int>? ExtraIds { get; set; }
    public string? Notes { get; set; }
}

public class CreateOrderRequest
{
    public string CustomerName { get; set; } = "";
    public string? Phone { get; set; }
    public OrderType Type { get; set; } = OrderType.Local;
    public string? DeliveryAddress { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Efectivo;
    public string? Notes { get; set; }
    public List<OrderLineRequest> Items { get; set; } = new();
}

public record UpdateStatusRequest(OrderStatus Status);

public record OrderItemDto(string ProductName, int Quantity, decimal UnitPrice, string? Notes, List<string> Extras);

public record OrderDto(int Id, string Number, string CustomerName, string? Phone, OrderType Type,
    string? DeliveryAddress, OrderStatus Status, PaymentMethod PaymentMethod, string? Notes,
    decimal DeliveryFee, decimal Total, DateTime CreatedAt, List<OrderItemDto> Items)
{
    public static OrderDto From(Order o) => new(
        o.Id, o.Number, o.CustomerName, o.Phone, o.Type, o.DeliveryAddress, o.Status, o.PaymentMethod,
        o.Notes, o.DeliveryFee, o.Total, o.CreatedAt,
        o.Items.Select(i => new OrderItemDto(i.ProductName, i.Quantity, i.UnitPrice, i.Notes,
            i.Extras.Select(e => e.Name).ToList())).ToList());
}

public record TrackingDto(string Number, OrderStatus Status, OrderType Type, DateTime CreatedAt, DateTime UpdatedAt);
