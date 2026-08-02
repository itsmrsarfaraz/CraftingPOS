using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Interfaces;
using Serilog;

namespace CraftingPOS.Application.Services;

public class ProductVariantService : IProductVariantService
{
    private readonly IProductVariantRepository _variantRepository;
    private readonly IProductRepository _productRepository;
    private readonly CurrentUserContext _currentUserContext;

    public ProductVariantService(
        IProductVariantRepository variantRepository,
        IProductRepository productRepository,
        CurrentUserContext currentUserContext)
    {
        _variantRepository = variantRepository;
        _productRepository = productRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<List<ProductVariantDto>> GetByProductIdAsync(int productId)
    {
        var variants = await _variantRepository.GetByProductIdAsync(productId);
        return variants.Select(MapToDto).ToList();
    }

    public async Task<OperationResult> SaveAsync(SaveProductVariantDto dto)
    {
        if (dto.ProductId <= 0)
            return OperationResult.Fail("The parent product must be saved before adding variants.");

        if (string.IsNullOrWhiteSpace(dto.VariantName))
            return OperationResult.Fail("Variant name is required (e.g. 'Small', 'Red - Large', '1 Liter', 'Sugar Free').");

        if (string.IsNullOrWhiteSpace(dto.Barcode))
            return OperationResult.Fail("Barcode is required.");

        if (string.IsNullOrWhiteSpace(dto.SKU))
            return OperationResult.Fail("SKU is required.");

        var excludeId = dto.Id > 0 ? dto.Id : (int?)null;

        if (await _productRepository.BarcodeExistsAsync(dto.Barcode.Trim())
            || await _variantRepository.BarcodeExistsAsync(dto.Barcode.Trim(), excludeId))
        {
            return OperationResult.Fail($"Barcode '{dto.Barcode}' is already in use.");
        }

        if (await _productRepository.SkuExistsAsync(dto.SKU.Trim())
            || await _variantRepository.SkuExistsAsync(dto.SKU.Trim(), excludeId))
        {
            return OperationResult.Fail($"SKU '{dto.SKU}' is already in use.");
        }

        var currentUsername = _currentUserContext.Session?.Username ?? "system";

        if (dto.Id == 0)
        {
            var variant = new ProductVariant
            {
                ProductId = dto.ProductId,
                VariantName = dto.VariantName.Trim(),
                Barcode = dto.Barcode.Trim(),
                SKU = dto.SKU.Trim(),
                CostPrice = dto.CostPrice,
                SellingPrice = dto.SellingPrice,
                CurrentStock = dto.CurrentStock,
                MinimumStock = dto.MinimumStock,
                CreatedBy = currentUsername
            };

            await _variantRepository.AddAsync(variant);
            await _variantRepository.SaveChangesAsync();

            Log.Information("Product variant '{VariantName}' created for ProductId {ProductId} by '{User}'.",
                variant.VariantName, dto.ProductId, currentUsername);
        }
        else
        {
            var variant = await _variantRepository.GetByIdAsync(dto.Id);
            if (variant == null)
                return OperationResult.Fail("Variant not found.");

            variant.VariantName = dto.VariantName.Trim();
            variant.Barcode = dto.Barcode.Trim();
            variant.SKU = dto.SKU.Trim();
            variant.CostPrice = dto.CostPrice;
            variant.SellingPrice = dto.SellingPrice;
            variant.CurrentStock = dto.CurrentStock;
            variant.MinimumStock = dto.MinimumStock;
            variant.UpdatedBy = currentUsername;

            await _variantRepository.UpdateAsync(variant);
            await _variantRepository.SaveChangesAsync();

            Log.Information("Product variant '{VariantName}' (Id: {Id}) updated by '{User}'.",
                variant.VariantName, variant.Id, currentUsername);
        }

        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeactivateAsync(int id)
    {
        var variant = await _variantRepository.GetByIdAsync(id);
        if (variant == null)
            return OperationResult.Fail("Variant not found.");

        variant.IsActive = false;
        variant.UpdatedBy = _currentUserContext.Session?.Username ?? "system";

        await _variantRepository.UpdateAsync(variant);
        await _variantRepository.SaveChangesAsync();

        Log.Information("Product variant '{VariantName}' (Id: {Id}) deactivated by '{User}'.",
            variant.VariantName, id, _currentUserContext.Session?.Username ?? "system");

        return OperationResult.Ok();
    }

    private static ProductVariantDto MapToDto(ProductVariant v)
    {
        return new ProductVariantDto
        {
            Id = v.Id,
            ProductId = v.ProductId,
            ProductName = v.Product?.Name ?? string.Empty,
            VariantName = v.VariantName,
            Barcode = v.Barcode,
            SKU = v.SKU,
            CostPrice = v.CostPrice,
            SellingPrice = v.SellingPrice,
            CurrentStock = v.CurrentStock,
            MinimumStock = v.MinimumStock,
            IsActive = v.IsActive
        };
    }
}