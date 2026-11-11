namespace PanchosLove.Api.Models;

public enum OrderType { Local, Delivery }
public enum OrderStatus { Pendiente, EnPreparacion, Listo, Entregado, Cancelado }
public enum PaymentMethod { Efectivo, Tarjeta, Transferencia, MercadoPago }

public static class Roles
{
    public const string Admin = "Admin";
    public const string Cajero = "Cajero";
    public const string Cocina = "Cocina";
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = Roles.Cajero;
}

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public List<Product> Products { get; set; } = new();
}

/// <summary>Producto vendible (pancho, bebida) o extra/topping (IsExtra = true).</summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsExtra { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public List<RecipeItem> Recipe { get; set; } = new();
}

/// <summary>Insumo de inventario: panes, salchichas, aderezos, toppings, bebidas.</summary>
public class Ingredient
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "un"; // un, g, ml
    public decimal Stock { get; set; }
    public decimal MinStock { get; set; }
    public decimal CostPerUnit { get; set; }
}

/// <summary>Cuánto insumo consume una unidad de un producto.</summary>
public class RecipeItem
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int IngredientId { get; set; }
    public Ingredient? Ingredient { get; set; }
    public decimal Quantity { get; set; }
}

public class StockMovement
{
    public int Id { get; set; }
    public int IngredientId { get; set; }
    public Ingredient Ingredient { get; set; } = null!;
    /// <summary>Positivo = ingreso, negativo = egreso.</summary>
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = "";
    public int? OrderId { get; set; }
    public Order? Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Order
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string? Phone { get; set; }
    public OrderType Type { get; set; }
    public string? DeliveryAddress { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pendiente;
    public PaymentMethod PaymentMethod { get; set; }
    public string? Notes { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    // Sin FK a Product a propósito: el historial no se rompe si se borra el producto.
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
    public List<OrderItemExtra> Extras { get; set; } = new();
}

public class OrderItemExtra
{
    public int Id { get; set; }
    public int OrderItemId { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
}
