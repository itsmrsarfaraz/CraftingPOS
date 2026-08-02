using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Interfaces;
using Serilog;

namespace CraftingPOS.Application.Services;

public class BrandService : IBrandService
{
    private readonly IBrandRepository _brandRepository;
    private readonly CurrentUserContext _currentUserContext;

    public BrandService(IBrandRepository brandRepository, CurrentUserContext currentUserContext)
    {
        _brandRepository = brandRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<List<BrandDto>> GetAllAsync()
    {
        var brands = await _brandRepository.GetAllAsync();
        return brands.OrderBy(b => b.Name).Select(MapToDto).ToList();
    }

    public async Task<OperationResult> SaveAsync(SaveBrandDto dto)
    {
        var name = dto.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            return OperationResult.Fail("Brand name is required.");
        }

        var excludeId = dto.Id > 0 ? dto.Id : (int?)null;
        if (await _brandRepository.ExistsByNameAsync(name, excludeId))
        {
            return OperationResult.Fail($"A brand named '{name}' already exists.");
        }

        var currentUsername = _currentUserContext.Session?.Username ?? "system";

        if (dto.Id == 0)
        {
            var brand = new Brand
            {
                Name = name,
                Description = dto.Description?.Trim(),
                CreatedBy = currentUsername
            };

            await _brandRepository.AddAsync(brand);
            await _brandRepository.SaveChangesAsync();

            Log.Information("Brand '{Name}' created by '{User}'.", name, currentUsername);
        }
        else
        {
            var brand = await _brandRepository.GetByIdAsync(dto.Id);
            if (brand == null)
            {
                return OperationResult.Fail("Brand not found.");
            }

            brand.Name = name;
            brand.Description = dto.Description?.Trim();
            brand.UpdatedBy = currentUsername;

            await _brandRepository.UpdateAsync(brand);
            await _brandRepository.SaveChangesAsync();

            Log.Information("Brand '{Name}' (Id: {Id}) updated by '{User}'.", name, dto.Id, currentUsername);
        }

        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeactivateAsync(int id)
    {
        var brand = await _brandRepository.GetByIdAsync(id);
        if (brand == null)
        {
            return OperationResult.Fail("Brand not found.");
        }

        brand.IsActive = false;
        brand.UpdatedBy = _currentUserContext.Session?.Username ?? "system";

        await _brandRepository.UpdateAsync(brand);
        await _brandRepository.SaveChangesAsync();

        Log.Information("Brand '{Name}' (Id: {Id}) deactivated by '{User}'.",
            brand.Name, id, _currentUserContext.Session?.Username ?? "system");

        return OperationResult.Ok();
    }

    private static BrandDto MapToDto(Brand b)
    {
        return new BrandDto
        {
            Id = b.Id,
            Name = b.Name,
            Description = b.Description,
            IsActive = b.IsActive,
            CreatedAt = b.CreatedAt
        };
    }
}