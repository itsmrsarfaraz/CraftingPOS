using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public interface IProductService
{
    Task<List<ProductDto>> GetAllAsync();
    Task<List<ProductDto>> SearchAsync(string searchTerm);
    Task<OperationResult> SaveAsync(SaveProductDto dto);
    Task<OperationResult> DeactivateAsync(int id);
    Task<int> CountAsync();
}