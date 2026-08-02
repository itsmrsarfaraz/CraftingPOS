using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Enums;
using CraftingPOS.Persistence;

namespace CraftingPOS.Tests.TestSupport;

/// <summary>Minimal, reusable seed data shared across test classes.</summary>
public static class TestSeed
{
    public static (Role ownerRole, Role cashierRole) SeedRoles(AppDbContext context)
    {
        var owner = new Role { Name = RoleNames.Owner };
        var cashier = new Role { Name = RoleNames.Cashier };
        context.Roles.AddRange(owner, cashier);
        context.SaveChanges();
        return (owner, cashier);
    }

    public static User SeedUser(AppDbContext context, Role role, string username, string passwordHash)
    {
        var user = new User
        {
            RoleId = role.Id,
            Username = username,
            FullName = username,
            PasswordHash = passwordHash,
            IsActive = true
        };
        context.Users.Add(user);
        context.SaveChanges();
        return user;
    }

    public static Category SeedCategory(AppDbContext context, string name = "Beverages")
    {
        var category = new Category { Name = name };
        context.Categories.Add(category);
        context.SaveChanges();
        return category;
    }

    public static Product SeedProduct(
        AppDbContext context, Category category, string name = "Pepsi",
        string barcode = "BC001", string sku = "SKU001",
        decimal costPrice = 100, decimal sellingPrice = 150, decimal stock = 50, decimal minStock = 5)
    {
        var product = new Product
        {
            CategoryId = category.Id,
            Name = name,
            Barcode = barcode,
            SKU = sku,
            ProductType = ProductType.Standard,
            CostPrice = costPrice,
            SellingPrice = sellingPrice,
            CurrentStock = stock,
            MinimumStock = minStock
        };
        context.Products.Add(product);
        context.SaveChanges();
        return product;
    }
}