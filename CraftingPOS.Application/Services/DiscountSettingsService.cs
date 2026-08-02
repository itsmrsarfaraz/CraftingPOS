using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Interfaces;
using Serilog;

namespace CraftingPOS.Application.Services;

public class DiscountSettingsService : IDiscountSettingsService
{
    private readonly IDiscountSettingsRepository _repository;
    private readonly CurrentUserContext _currentUserContext;

    public DiscountSettingsService(IDiscountSettingsRepository repository, CurrentUserContext currentUserContext)
    {
        _repository = repository;
        _currentUserContext = currentUserContext;
    }

    public async Task<DiscountSettingsDto> GetAsync()
    {
        var settings = await _repository.GetOrCreateAsync();
        return new DiscountSettingsDto
        {
            MaxCashierDiscountPercent = settings.MaxCashierDiscountPercent,
            MaxCashierDiscountFlat = settings.MaxCashierDiscountFlat
        };
    }

    public async Task<OperationResult> SaveAsync(DiscountSettingsDto dto)
    {
        var role = _currentUserContext.Session?.RoleName;
        if (role != CraftingPOS.Domain.Enums.RoleNames.Owner && role != CraftingPOS.Domain.Enums.RoleNames.SystemAdmin)
        {
            return OperationResult.Fail("Only an Owner can configure discount limits.");
        }

        if (dto.MaxCashierDiscountPercent < 0 || dto.MaxCashierDiscountPercent > 100)
        {
            return OperationResult.Fail("Percentage limit must be between 0 and 100.");
        }

        if (dto.MaxCashierDiscountFlat < 0)
        {
            return OperationResult.Fail("Flat limit cannot be negative.");
        }

        var settings = await _repository.GetOrCreateAsync();
        settings.MaxCashierDiscountPercent = dto.MaxCashierDiscountPercent;
        settings.MaxCashierDiscountFlat = dto.MaxCashierDiscountFlat;
        settings.UpdatedBy = _currentUserContext.Session?.Username ?? "system";

        await _repository.UpdateAsync(settings);
        await _repository.SaveChangesAsync();

        Log.Information("Discount limits updated: {Percent}% / Rs.{Flat} by '{User}'.",
            settings.MaxCashierDiscountPercent, settings.MaxCashierDiscountFlat, _currentUserContext.Session?.Username);

        return OperationResult.Ok();
    }
}