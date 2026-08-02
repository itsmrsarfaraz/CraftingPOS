using CraftingPOS.Domain.Common;

namespace CraftingPOS.Domain.Entities;

/// <summary>
/// Single-row table (Owner/SystemAdmin configurable) defining how far a
/// Cashier may discount a sale at checkout before Owner authorization
/// is required (FR-DISC-006/007/008).
/// </summary>
public class DiscountSettings : BaseEntity
{
    public decimal MaxCashierDiscountPercent { get; set; } = 5m;
    public decimal MaxCashierDiscountFlat { get; set; } = 500m;
}