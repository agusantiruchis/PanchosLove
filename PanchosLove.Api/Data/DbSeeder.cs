using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PanchosLove.Api.Models;

namespace PanchosLove.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        await db.Database.EnsureCreatedAsync();
        if (await db.Users.AnyAsync()) return;

        // Usuarios de demostración: CAMBIAR las contraseñas antes de producción.
        User U(string name, string email, string pass, string role)
        {
            var u = new User { Name = name, Email = email, Role = role };
            u.PasswordHash = hasher.HashPassword(u, pass);
            return u;
        }
        db.Users.AddRange(
            U("Administrador", "admin@panchoslove.com", "Admin123!", Roles.Admin),
            U("Cajero", "cajero@panchoslove.com", "Cajero123!", Roles.Cajero),
            U("Cocina", "cocina@panchoslove.com", "Cocina123!", Roles.Cocina));

        // Insumos
        Ingredient I(string n, string unit, decimal stock, decimal min, decimal cost) =>
            new() { Name = n, Unit = unit, Stock = stock, MinStock = min, CostPerUnit = cost };

        var pan = I("Pan de pancho", "un", 200, 30, 120);
        var salchicha = I("Salchicha clásica", "un", 200, 30, 250);
        var salchichaPrem = I("Salchicha premium", "un", 100, 20, 450);
        var mayo = I("Mayonesa", "g", 3000, 500, 4);
        var ketchup = I("Ketchup", "g", 3000, 500, 4);
        var mostaza = I("Mostaza", "g", 2000, 300, 5);
        var cheddar = I("Cheddar fundido", "g", 4000, 600, 12);
        var panceta = I("Panceta crocante", "g", 2500, 400, 20);
        var papas = I("Papas pay", "g", 3000, 500, 8);
        var cebolla = I("Cebolla crispy", "g", 1500, 300, 9);
        var gaseosa = I("Gaseosa lata", "un", 120, 24, 600);
        var agua = I("Agua mineral", "un", 100, 20, 400);
        db.Ingredients.AddRange(pan, salchicha, salchichaPrem, mayo, ketchup, mostaza, cheddar, panceta,
            papas, cebolla, gaseosa, agua);

        // Categorías
        var cPanchos = new Category { Name = "Panchos", SortOrder = 1 };
        var cBebidas = new Category { Name = "Bebidas", SortOrder = 2 };
        var cExtras = new Category { Name = "Extras y toppings", SortOrder = 3 };
        db.Categories.AddRange(cPanchos, cBebidas, cExtras);

        // Productos
        Product P(string name, string desc, decimal price, Category cat, bool extra, params (Ingredient i, decimal q)[] recipe)
        {
            var p = new Product { Name = name, Description = desc, Price = price, Category = cat, IsExtra = extra };
            foreach (var (ing, q) in recipe) p.Recipe.Add(new RecipeItem { Ingredient = ing, Quantity = q });
            return p;
        }
        db.Products.AddRange(
            P("Clásico", "Pancho con mayonesa, ketchup y mostaza", 3500, cPanchos, false,
                (pan, 1), (salchicha, 1), (mayo, 10), (ketchup, 10), (mostaza, 5)),
            P("Cheddar & Panceta", "Salchicha con cheddar fundido y panceta crocante", 5200, cPanchos, false,
                (pan, 1), (salchicha, 1), (cheddar, 30), (panceta, 20)),
            P("Super Love", "Salchicha premium, cheddar, papas pay y cebolla crispy", 6500, cPanchos, false,
                (pan, 1), (salchichaPrem, 1), (cheddar, 25), (papas, 20), (cebolla, 15), (mayo, 10)),
            P("Gaseosa lata", "Lata 354 ml", 1800, cBebidas, false, (gaseosa, 1)),
            P("Agua mineral", "Botella 500 ml", 1500, cBebidas, false, (agua, 1)),
            P("Extra cheddar", "Porción adicional de cheddar fundido", 900, cExtras, true, (cheddar, 25)),
            P("Extra panceta", "Porción adicional de panceta", 1100, cExtras, true, (panceta, 20)),
            P("Extra papas pay", "Porción adicional de papas pay", 700, cExtras, true, (papas, 20)));

        await db.SaveChangesAsync();
    }
}
