using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public interface IProductDiscountService
{
    Task<ProductDiscountDto> GetForProductAsync(int productId);
    Task<OperationResult> SetDiscountAsync(SaveProductDiscountDto dto);
    Task<OperationResult> RemoveDiscountAsync(int productId);
}