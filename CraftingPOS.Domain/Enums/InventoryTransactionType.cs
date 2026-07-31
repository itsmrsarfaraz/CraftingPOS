namespace CraftingPOS.Domain.Enums;

public enum InventoryTransactionType
{
    OpeningStock = 0,
    Purchase = 1,
    Sale = 2,
    SaleReturn = 3,
    Damage = 4,
    Adjustment = 5
}