using Microsoft.EntityFrameworkCore;
using PanchosLove.Api.Models;

namespace PanchosLove.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<RecipeItem> RecipeItems => Set<RecipeItem>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemExtra> OrderItemExtras => Set<OrderItemExtra>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
        b.Entity<Order>().HasIndex(o => o.Number).IsUnique();
        b.Entity<Order>().HasIndex(o => o.CreatedAt);
        b.Entity<RecipeItem>().HasIndex(r => new { r.ProductId, r.IngredientId }).IsUnique();

        b.Entity<Order>().Property(o => o.Status).HasConversion<string>();
        b.Entity<Order>().Property(o => o.Type).HasConversion<string>();
        b.Entity<Order>().Property(o => o.PaymentMethod).HasConversion<string>();

        b.Entity<Product>().HasOne(p => p.Category).WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Product>().HasMany(p => p.Recipe).WithOne(r => r.Product)
            .HasForeignKey(r => r.ProductId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<RecipeItem>().HasOne(r => r.Ingredient).WithMany()
            .HasForeignKey(r => r.IngredientId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<StockMovement>().HasOne(m => m.Order).WithMany()
            .HasForeignKey(m => m.OrderId).OnDelete(DeleteBehavior.SetNull);
    }
}
