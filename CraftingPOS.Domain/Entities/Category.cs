using CraftingPOS.Domain.Common;

namespace CraftingPOS.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}