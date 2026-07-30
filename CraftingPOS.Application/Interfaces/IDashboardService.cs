using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync();
}