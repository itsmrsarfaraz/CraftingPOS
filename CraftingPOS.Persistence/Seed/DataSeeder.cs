using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CraftingPOS.Persistence.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext context, Func<string, string> hashPassword)
    {
        await SeedRolesAsync(context);
        await SeedDefaultAccountsAsync(context, hashPassword);
    }

    private static async Task SeedRolesAsync(AppDbContext context)
    {
        var existingNames = await context.Roles.Select(r => r.Name).ToListAsync();

        if (!existingNames.Contains(RoleNames.SystemAdmin))
            context.Roles.Add(new Role { Name = RoleNames.SystemAdmin, Description = "Full system access, creates Owner and Cashier accounts." });

        if (!existingNames.Contains(RoleNames.Owner))
            context.Roles.Add(new Role { Name = RoleNames.Owner, Description = "Full business access. Creates Cashier accounts." });

        if (!existingNames.Contains(RoleNames.Cashier))
            context.Roles.Add(new Role { Name = RoleNames.Cashier, Description = "Limited access to sales operations." });

        await context.SaveChangesAsync();
    }

    private static async Task SeedDefaultAccountsAsync(AppDbContext context, Func<string, string> hashPassword)
    {
        if (await context.Users.AnyAsync()) return;

        var systemAdminRole = await context.Roles.FirstAsync(r => r.Name == RoleNames.SystemAdmin);
        var ownerRole = await context.Roles.FirstAsync(r => r.Name == RoleNames.Owner);

        context.Users.Add(new User
        {
            RoleId = systemAdminRole.Id,
            Username = "sysadmin",
            FullName = "System Administrator",
            PasswordHash = hashPassword("SysAdmin@123"),
            IsActive = true
        });

        context.Users.Add(new User
        {
            RoleId = ownerRole.Id,
            Username = "admin",
            FullName = "System Owner",
            PasswordHash = hashPassword("Admin@123"),
            IsActive = true
        });

        await context.SaveChangesAsync();
    }
}