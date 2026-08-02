namespace CraftingPOS.Application.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SaveCategoryDto
{
    public int Id { get; set; } // 0 = create new, >0 = update existing
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}