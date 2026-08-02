using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();
    Task<OperationResult> SaveAsync(SaveCategoryDto dto);
    Task<OperationResult> DeactivateAsync(int id);
}