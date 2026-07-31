using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public interface IPurchaseService
{
    Task<List<PurchaseDto>> GetAllAsync();
    Task<OperationResult<int>> SaveAsync(SavePurchaseDto dto);
}