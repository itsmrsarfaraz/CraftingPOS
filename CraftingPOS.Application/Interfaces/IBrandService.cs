using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public interface IBrandService
{
    Task<List<BrandDto>> GetAllAsync();
    Task<OperationResult> SaveAsync(SaveBrandDto dto);
    Task<OperationResult> DeactivateAsync(int id);
}