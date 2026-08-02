using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public interface IDiscountSettingsService
{
    Task<DiscountSettingsDto> GetAsync();
    Task<OperationResult> SaveAsync(DiscountSettingsDto dto);
}