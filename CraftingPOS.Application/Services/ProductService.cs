using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Enums;
using CraftingPOS.Domain.Interfaces;
using Serilog;

namespace CraftingPOS.Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IProductDiscountRepository _productDiscountRepository;
    private readonly IImageStorageService _imageStorageService;
    private readonly CurrentUserContext _currentUserContext;

    public ProductService(
        IProductRepository productRepository,
        IProductDiscountRepository productDiscountRepository,
        IImageStorageService imageStorageService,
        CurrentUserContext currentUserContext)
    {
        _productRepository = productRepository;
        _productDiscountRepository = productDiscountRepository;
        _imageStorageService = imageStorageService;
        _currentUserContext = currentUserContext;
    }

    public async Task<List<ProductDto>> GetAllAsync()
    {
        var products = await _productRepository.GetAllAsync();
        var result = new List<ProductDto>();
        foreach (var p in products.OrderBy(p => p.Name))
            result.Add(await MapToDtoAsync(p));
        return result;
    }

    public async Task<List<ProductDto>> SearchAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm)) return await GetAllAsync();

        var products = await _productRepository.SearchAsync(searchTerm.Trim());
        var result = new List<ProductDto>();
        foreach (var p in products.OrderBy(p => p.Name))
            result.Add(await MapToDtoAsync(p));
        return result;
    }

    public async Task<int> CountAsync() => await _productRepository.CountAsync();

    public async Task<OperationResult<int>> SaveAsync(SaveProductDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return OperationResult<int>.Fail("Product name is required.");
        if (dto.CategoryId <= 0)
            return OperationResult<int>.Fail("Please select a category.");
        if (string.IsNullOrWhiteSpace(dto.Barcode))
            return OperationResult<int>.Fail("Barcode is required.");
        if (string.IsNullOrWhiteSpace(dto.SKU))
            return OperationResult<int>.Fail("SKU is required.");

        var isOwner = _currentUserContext.Session?.RoleName is RoleNames.Owner or RoleNames.SystemAdmin;
        if (dto.SellingPrice < dto.CostPrice && !(dto.AllowPriceOverride && isOwner))
        {
            return OperationResult<int>.Fail("Selling price cannot be less than cost price. An Owner may override this.");
        }

        var excludeId = dto.Id > 0 ? dto.Id : (int?)null;

        if (await _productRepository.BarcodeExistsAsync(dto.Barcode.Trim(), excludeId))
            return OperationResult<int>.Fail($"Barcode '{dto.Barcode}' is already assigned to another product.");
        if (await _productRepository.SkuExistsAsync(dto.SKU.Trim(), excludeId))
            return OperationResult<int>.Fail($"SKU '{dto.SKU}' is already assigned to another product.");

        var currentUsername = _currentUserContext.Session?.Username ?? "system";

        string? imagePath = null;
        if (!string.IsNullOrWhiteSpace(dto.NewImageSourcePath))
            imagePath = await _imageStorageService.SaveProductImageAsync(dto.NewImageSourcePath);

        if (dto.Id == 0)
        {
            var product = new Product
            {
                CategoryId = dto.CategoryId,
                BrandId = dto.BrandId,
                Barcode = dto.Barcode.Trim(),
                SKU = dto.SKU.Trim(),
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                ProductType = dto.ProductType,
                CostPrice = dto.CostPrice,
                SellingPrice = dto.SellingPrice,
                CurrentStock = dto.CurrentStock,
                MinimumStock = dto.MinimumStock,
                ImagePath = imagePath,
                CreatedBy = currentUsername
            };

            await _productRepository.AddAsync(product);
            await _productRepository.SaveChangesAsync();

            Log.Information("Product '{Name}' created by '{User}'.", product.Name, currentUsername);
            return OperationResult<int>.Ok(product.Id);
        }
        else
        {
            var product = await _productRepository.GetByIdAsync(dto.Id);
            if (product == null) return OperationResult<int>.Fail("Product not found.");

            product.CategoryId = dto.CategoryId;
            product.BrandId = dto.BrandId;
            product.Barcode = dto.Barcode.Trim();
            product.SKU = dto.SKU.Trim();
            product.Name = dto.Name.Trim();
            product.Description = dto.Description?.Trim();
            product.ProductType = dto.ProductType;
            product.CostPrice = dto.CostPrice;
            product.SellingPrice = dto.SellingPrice;
            product.CurrentStock = dto.CurrentStock;
            product.MinimumStock = dto.MinimumStock;
            product.UpdatedBy = currentUsername;

            if (imagePath != null)
            {
                if (!string.IsNullOrWhiteSpace(product.ImagePath))
                    _imageStorageService.DeleteProductImage(product.ImagePath);
                product.ImagePath = imagePath;
            }

            await _productRepository.UpdateAsync(product);
            await _productRepository.SaveChangesAsync();

            Log.Information("Product '{Name}' (Id: {Id}) updated by '{User}'.", product.Name, product.Id, currentUsername);
            return OperationResult<int>.Ok(product.Id);
        }
    }

    public async Task<OperationResult> DeactivateAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null) return OperationResult.Fail("Product not found.");

        product.IsActive = false;
        product.UpdatedBy = _currentUserContext.Session?.Username ?? "system";

        await _productRepository.UpdateAsync(product);
        await _productRepository.SaveChangesAsync();

        Log.Information("Product '{Name}' (Id: {Id}) deactivated by '{User}'.", product.Name, id, _currentUserContext.Session?.Username);
        return OperationResult.Ok();
    }

    private async Task<ProductDto> MapToDtoAsync(Product p)
    {
        var discount = await _productDiscountRepository.GetByProductIdAsync(p.Id);

        return new ProductDto
        {
            Id = p.Id,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? string.Empty,
            BrandId = p.BrandId,
            BrandName = p.Brand?.Name,
            Barcode = p.Barcode,
            SKU = p.SKU,
            Name = p.Name,
            Description = p.Description,
            ProductType = p.ProductType,
            CostPrice = p.CostPrice,
            SellingPrice = p.SellingPrice,
            CurrentStock = p.CurrentStock,
            MinimumStock = p.MinimumStock,
            ImagePath = p.ImagePath,
            ImageFullPath = string.IsNullOrWhiteSpace(p.ImagePath) ? null : _imageStorageService.GetFullPath(p.ImagePath),
            IsActive = p.IsActive,
            DiscountType = discount?.DiscountType,
            DiscountValue = discount?.DiscountValue
        };
    }
}