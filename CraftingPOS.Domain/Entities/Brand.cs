using CraftingPOS.Domain.Common;

namespace CraftingPOS.Domain.Entities;

public class Brand : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}