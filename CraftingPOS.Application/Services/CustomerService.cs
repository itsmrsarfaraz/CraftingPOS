using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Interfaces;
using Serilog;

namespace CraftingPOS.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly CurrentUserContext _currentUserContext;

    public CustomerService(ICustomerRepository customerRepository, CurrentUserContext currentUserContext)
    {
        _customerRepository = customerRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task<List<CustomerDto>> GetAllAsync()
    {
        var customers = await _customerRepository.GetAllAsync();
        return customers.OrderBy(c => c.Name).Select(MapToDto).ToList();
    }

    public async Task<List<CustomerDto>> SearchAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllAsync();
        }

        var customers = await _customerRepository.SearchAsync(searchTerm.Trim());
        return customers.OrderBy(c => c.Name).Select(MapToDto).ToList();
    }

    public async Task<int> CountAsync()
    {
        return await _customerRepository.CountAsync();
    }

    public async Task<OperationResult> SaveAsync(SaveCustomerDto dto)
    {
        var name = dto.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            return OperationResult.Fail("Customer name is required.");
        }

        // Phone is optional, but if provided, warn on duplicates (soft rule —
        // unlike Category/Brand/Supplier names, we don't hard-block this,
        // since a household may share one phone across multiple customers).
        if (!string.IsNullOrWhiteSpace(dto.Phone))
        {
            var excludeId = dto.Id > 0 ? dto.Id : (int?)null;
            var duplicatePhone = await _customerRepository.ExistsByPhoneAsync(dto.Phone.Trim(), excludeId);

            if (duplicatePhone)
            {
                return OperationResult.Fail(
                    $"Another customer is already registered with phone number '{dto.Phone}'. " +
                    "Please verify before saving, or use a different number.");
            }
        }

        var currentUsername = _currentUserContext.Session?.Username ?? "system";

        if (dto.Id == 0)
        {
            // FR-CUST-001: create
            var customer = new Customer
            {
                Name = name,
                Phone = dto.Phone?.Trim(),
                Email = dto.Email?.Trim(),
                Address = dto.Address?.Trim(),
                Notes = dto.Notes?.Trim(),
                CreatedBy = currentUsername
            };

            await _customerRepository.AddAsync(customer);
            await _customerRepository.SaveChangesAsync();

            Log.Information("Customer '{Name}' created by '{User}'.", name, currentUsername);
        }
        else
        {
            // FR-CUST-002: edit
            var customer = await _customerRepository.GetByIdAsync(dto.Id);
            if (customer == null)
            {
                return OperationResult.Fail("Customer not found.");
            }

            customer.Name = name;
            customer.Phone = dto.Phone?.Trim();
            customer.Email = dto.Email?.Trim();
            customer.Address = dto.Address?.Trim();
            customer.Notes = dto.Notes?.Trim();
            customer.UpdatedBy = currentUsername;

            await _customerRepository.UpdateAsync(customer);
            await _customerRepository.SaveChangesAsync();

            Log.Information("Customer '{Name}' (Id: {Id}) updated by '{User}'.", name, dto.Id, currentUsername);
        }

        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeactivateAsync(int id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);
        if (customer == null)
        {
            return OperationResult.Fail("Customer not found.");
        }

        // FR-CUST-003: deactivate (soft delete)
        customer.IsActive = false;
        customer.UpdatedBy = _currentUserContext.Session?.Username ?? "system";

        await _customerRepository.UpdateAsync(customer);
        await _customerRepository.SaveChangesAsync();

        Log.Information("Customer '{Name}' (Id: {Id}) deactivated by '{User}'.",
            customer.Name, id, _currentUserContext.Session?.Username ?? "system");

        return OperationResult.Ok();
    }

    public Task<List<SalesHistoryItemDto>> GetSalesHistoryAsync(int customerId)
    {
        // TODO (Sprint 10 - Sales): replace with a real query against Sales
        // filtered by CustomerId, ordered by SaleDate descending.
        return Task.FromResult(new List<SalesHistoryItemDto>());
    }

    private static CustomerDto MapToDto(Customer c)
    {
        return new CustomerDto
        {
            Id = c.Id,
            Name = c.Name,
            Phone = c.Phone,
            Email = c.Email,
            Address = c.Address,
            Notes = c.Notes,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt
        };
    }
}