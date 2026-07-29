using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CraftingPOS.Persistence.Seed;

/// <summary>
/// Seeds Roles and a default Owner account on first run.
/// Password hashing is injected as a delegate to keep this layer
/// independent of the Infrastructure project (Clean Architecture rule).
/// Default login: admin / Admin@123 — change immediately after first login.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context, Func<string, string> hashPassword)
    {
        await SeedRolesAsync(context);
        await SeedDefaultOwnerAsync(context, hashPassword);
    }

    private static async Task SeedRolesAsync(AppDbContext context)
    {
        if (!await context.Roles.AnyAsync())
        {
            context.Roles.AddRange(
                new Role { Name = RoleNames.Owner, Description = "Full system access." },
                new Role { Name = RoleNames.Cashier, Description = "Limited access to sales operations." }
            );

            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedDefaultOwnerAsync(AppDbContext context, Func<string, string> hashPassword)
    {
        if (!await context.Users.AnyAsync())
        {
            var ownerRole = await context.Roles.FirstAsync(r => r.Name == RoleNames.Owner);

            var defaultOwner = new User
            {
                RoleId = ownerRole.Id,
                Username = "admin",
                FullName = "System Owner",
                PasswordHash = hashPassword("Admin@123"),
                IsActive = true
            };

            context.Users.Add(defaultOwner);
            await context.SaveChangesAsync();
        }
    }
}