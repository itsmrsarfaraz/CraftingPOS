using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public interface IProductVariantService
{
    Task<List<ProductVariantDto>> GetByProductIdAsync(int productId);
    Task<OperationResult> SaveAsync(SaveProductVariantDto dto);
    Task<OperationResult> DeactivateAsync(int id);
}