using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Interfaces;
using Serilog;

namespace CraftingPOS.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly CurrentUserContext _currentUserContext;

    public CategoryService(ICategoryRepository categoryRepository, CurrentUserContext currentUserContext)
    {
        _categoryRepository = categoryRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();

        return categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .ToList();
    }

    public async Task<OperationResult> SaveAsync(SaveCategoryDto dto)
    {
        var name = dto.Name?.Trim() ?? string.Empty;

        // FR-CAT-004: prevent duplicate category names
        if (string.IsNullOrWhiteSpace(name))
        {
            return OperationResult.Fail("Category name is required.");
        }

        var excludeId = dto.Id > 0 ? dto.Id : (int?)null;
        var duplicateExists = await _categoryRepository.ExistsByNameAsync(name, excludeId);

        if (duplicateExists)
        {
            return OperationResult.Fail($"A category named '{name}' already exists.");
        }

        var currentUsername = _currentUserContext.Session?.Username ?? "system";

        if (dto.Id == 0)
        {
            // FR-CAT-001: create
            var category = new Category
            {
                Name = name,
                Description = dto.Description?.Trim(),
                CreatedBy = currentUsername
            };

            await _categoryRepository.AddAsync(category);
            await _categoryRepository.SaveChangesAsync();

            Log.Information("Category '{Name}' created by '{User}'.", name, currentUsername);
        }
        else
        {
            // FR-CAT-002: edit
            var category = await _categoryRepository.GetByIdAsync(dto.Id);

            if (category == null)
            {
                return OperationResult.Fail("Category not found.");
            }

            category.Name = name;
            category.Description = dto.Description?.Trim();
            category.UpdatedBy = currentUsername;

            await _categoryRepository.UpdateAsync(category);
            await _categoryRepository.SaveChangesAsync();

            Log.Information("Category '{Name}' (Id: {Id}) updated by '{User}'.", name, dto.Id, currentUsername);
        }

        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeactivateAsync(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);

        if (category == null)
        {
            return OperationResult.Fail("Category not found.");
        }

        // FR-CAT-003: deactivate (soft delete — handled by BaseEntity.IsActive + global query filter)
        category.IsActive = false;
        category.UpdatedBy = _currentUserContext.Session?.Username ?? "system";

        await _categoryRepository.UpdateAsync(category);
        await _categoryRepository.SaveChangesAsync();

        Log.Information("Category '{Name}' (Id: {Id}) deactivated by '{User}'.",
            category.Name, id, _currentUserContext.Session?.Username ?? "system");

        return OperationResult.Ok();
    }
}