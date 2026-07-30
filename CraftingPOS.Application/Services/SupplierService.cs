using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Interfaces;
using Serilog;

namespace CraftingPOS.Application.Services;

public class SupplierService : ISupplierService
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly CurrentUserContext _currentUserContext;

    public SupplierService(ISupplierRepository supplierRepository, CurrentUserContext currentUserContext)
    {
        _supplierRepository = supplierRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<List<SupplierDto>> GetAllAsync()
    {
        var suppliers = await _supplierRepository.GetAllAsync();
        return suppliers.OrderBy(s => s.Name).Select(MapToDto).ToList();
    }

    public async Task<int> CountAsync()
    {
        return await _supplierRepository.CountAsync();
    }

    public async Task<OperationResult> SaveAsync(SaveSupplierDto dto)
    {
        var name = dto.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            return OperationResult.Fail("Supplier name is required.");
        }

        var excludeId = dto.Id > 0 ? dto.Id : (int?)null;
        if (await _supplierRepository.ExistsByNameAsync(name, excludeId))
        {
            return OperationResult.Fail($"A supplier named '{name}' already exists.");
        }

        var currentUsername = _currentUserContext.Session?.Username ?? "system";

        if (dto.Id == 0)
        {
            // FR-SUP-001: create
            var supplier = new Supplier
            {
                Name = name,
                ContactPerson = dto.ContactPerson?.Trim(),
                Phone = dto.Phone?.Trim(),
                Email = dto.Email?.Trim(),
                Address = dto.Address?.Trim(),
                Notes = dto.Notes?.Trim(),
                CreatedBy = currentUsername
            };

            await _supplierRepository.AddAsync(supplier);
            await _supplierRepository.SaveChangesAsync();

            Log.Information("Supplier '{Name}' created by '{User}'.", name, currentUsername);
        }
        else
        {
            // FR-SUP-002: edit
            var supplier = await _supplierRepository.GetByIdAsync(dto.Id);
            if (supplier == null)
            {
                return OperationResult.Fail("Supplier not found.");
            }

            supplier.Name = name;
            supplier.ContactPerson = dto.ContactPerson?.Trim();
            supplier.Phone = dto.Phone?.Trim();
            supplier.Email = dto.Email?.Trim();
            supplier.Address = dto.Address?.Trim();
            supplier.Notes = dto.Notes?.Trim();
            supplier.UpdatedBy = currentUsername;

            await _supplierRepository.UpdateAsync(supplier);
            await _supplierRepository.SaveChangesAsync();

            Log.Information("Supplier '{Name}' (Id: {Id}) updated by '{User}'.", name, dto.Id, currentUsername);
        }

        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeactivateAsync(int id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);
        if (supplier == null)
        {
            return OperationResult.Fail("Supplier not found.");
        }

        // FR-SUP-003: deactivate (soft delete)
        supplier.IsActive = false;
        supplier.UpdatedBy = _currentUserContext.Session?.Username ?? "system";

        await _supplierRepository.UpdateAsync(supplier);
        await _supplierRepository.SaveChangesAsync();

        Log.Information("Supplier '{Name}' (Id: {Id}) deactivated by '{User}'.",
            supplier.Name, id, _currentUserContext.Session?.Username ?? "system");

        return OperationResult.Ok();
    }

    public Task<List<PurchaseHistoryItemDto>> GetPurchaseHistoryAsync(int supplierId)
    {
        // TODO (Sprint 6 - Purchases): replace with a real query against Purchases
        // filtered by SupplierId, ordered by PurchaseDate descending.
        return Task.FromResult(new List<PurchaseHistoryItemDto>());
    }

    private static SupplierDto MapToDto(Supplier s)
    {
        return new SupplierDto
        {
            Id = s.Id,
            Name = s.Name,
            ContactPerson = s.ContactPerson,
            Phone = s.Phone,
            Email = s.Email,
            Address = s.Address,
            Notes = s.Notes,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt
        };
    }
}