using CraftingPOS.Domain.Common;

namespace CraftingPOS.Domain.Entities;

/// <summary>
/// Immutable audit trail entry. Per FR-AUDIT-001/002 this is never
/// updated or deleted — only ever inserted, mirroring the append-only
/// pattern already used for InventoryTransaction and CustomerLedger.
/// </summary>
public class AuditLog : BaseEntity
{
    public string Username { get; set; } = string.Empty; // denormalized: survives if the User row is later deactivated
    public string Module { get; set; } = string.Empty;    // e.g. "Products", "Users", "Sales", "Inventory"
    public string Action { get; set; } = string.Empty;    // e.g. "Create", "Update", "Deactivate", "PriceChange", "Login"
    public string Description { get; set; } = string.Empty;
}