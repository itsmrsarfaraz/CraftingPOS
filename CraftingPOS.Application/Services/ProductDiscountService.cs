using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Interfaces;
using Serilog;

namespace CraftingPOS.Application.Services;

public class ProductDiscountService : IProductDiscountService
{
    private readonly IProductDiscountRepository _discountRepository;
    private readonly CurrentUserContext _currentUserContext;

    public ProductDiscountService(IProductDiscountRepository discountRepository, CurrentUserContext currentUserContext)
    {
        _discountRepository = discountRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<ProductDiscountDto> GetForProductAsync(int productId)
    {
        var discount = await _discountRepository.GetByProductIdAsync(productId);

        return new ProductDiscountDto
        {
            ProductId = productId,
            DiscountType = discount?.DiscountType,
            DiscountValue = discount?.DiscountValue
        };
    }

    public async Task<OperationResult> SetDiscountAsync(SaveProductDiscountDto dto)
    {
        // FR-DISC-001: only Owner (or SystemAdmin) configures product discounts.
        var role = _currentUserContext.Session?.RoleName;
        if (role != CraftingPOS.Domain.Enums.RoleNames.Owner && role != CraftingPOS.Domain.Enums.RoleNames.SystemAdmin)
        {
            return OperationResult.Fail("Only an Owner can configure product discounts.");
        }

        if (dto.DiscountValue <= 0)
        {
            return OperationResult.Fail("Discount value must be greater than zero.");
        }

        if (dto.DiscountType == CraftingPOS.Domain.Enums.DiscountType.Percentage && dto.DiscountValue >= 100)
        {
            return OperationResult.Fail("Percentage discount must be less than 100%.");
        }

        var currentUsername = _currentUserContext.Session?.Username ?? "system";
        var existing = await _discountRepository.GetByProductIdAsync(dto.ProductId);

        if (existing == null)
        {
            var discount = new ProductDiscount
            {
                ProductId = dto.ProductId,
                DiscountType = dto.DiscountType,
                DiscountValue = dto.DiscountValue,
                CreatedBy = currentUsername
            };

            await _discountRepository.AddAsync(discount);
        }
        else
        {
            existing.DiscountType = dto.DiscountType;
            existing.DiscountValue = dto.DiscountValue;
            existing.IsActive = true;
            existing.UpdatedBy = currentUsername;

            await _discountRepository.UpdateAsync(existing);
        }

        await _discountRepository.SaveChangesAsync();

        Log.Information("Product discount set for ProductId {ProductId}: {Type} {Value} by '{User}'.",
            dto.ProductId, dto.DiscountType, dto.DiscountValue, currentUsername);

        return OperationResult.Ok();
    }

    public async Task<OperationResult> RemoveDiscountAsync(int productId)
    {
        var role = _currentUserContext.Session?.RoleName;
        if (role != CraftingPOS.Domain.Enums.RoleNames.Owner && role != CraftingPOS.Domain.Enums.RoleNames.SystemAdmin)
        {
            return OperationResult.Fail("Only an Owner can remove product discounts.");
        }

        var existing = await _discountRepository.GetByProductIdAsync(productId);
        if (existing == null) return OperationResult.Ok(); // nothing to remove

        existing.IsActive = false;
        existing.UpdatedBy = _currentUserContext.Session?.Username ?? "system";

        await _discountRepository.UpdateAsync(existing);
        await _discountRepository.SaveChangesAsync();

        return OperationResult.Ok();
    }
}