using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public static class AuditModules
{
    public const string Auth = "Authentication";
    public const string Products = "Products";
    public const string Inventory = "Inventory";
    public const string Discounts = "Discounts";
    public const string Users = "Users";
    public const string Sales = "Sales";
    public const string Backup = "Backup";
}

public interface IAuditLogService
{
    /// <summary>FR-AUDIT-001: writes one audit entry. Fire-and-forget safe — callers await it but a failure here should never break the calling operation.</summary>
    Task LogAsync(string module, string action, string description);

    /// <summary>FR-AUDIT-002: searchable audit history for the Audit Logs screen.</summary>
    Task<List<AuditLogDto>> SearchAsync(AuditLogSearchDto criteria);
}