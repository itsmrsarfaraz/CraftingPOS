namespace CraftingPOS.Application.DTOs;

public class BrandDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SaveBrandDto
{
    public int Id { get; set; } // 0 = create
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}