using CraftingPOS.Domain.Common;
using CraftingPOS.Domain.Enums;

namespace CraftingPOS.Domain.Entities;

public class Payment : BaseEntity
{
    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;

    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
}