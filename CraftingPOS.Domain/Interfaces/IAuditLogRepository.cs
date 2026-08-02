using CraftingPOS.Domain.Entities;

namespace CraftingPOS.Domain.Interfaces;

public interface IAuditLogRepository
{
    Task<List<AuditLog>> SearchAsync(string? module, string? username, DateTime? fromDate, DateTime? toDate, string? keyword);
    Task AddAsync(AuditLog entry);
    Task SaveChangesAsync();
}