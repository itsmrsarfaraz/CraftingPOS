using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CraftingPOS.Persistence.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;

    public AuditLogRepository(AppDbContext context) => _context = context;

    public async Task<List<AuditLog>> SearchAsync(
        string? module, string? username, DateTime? fromDate, DateTime? toDate, string? keyword)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(module))
            query = query.Where(a => a.Module == module);

        if (!string.IsNullOrWhiteSpace(username))
            query = query.Where(a => a.Username.ToLower().Contains(username.ToLower()));

        if (fromDate.HasValue)
            query = query.Where(a => a.CreatedAt >= fromDate.Value.Date);

        if (toDate.HasValue)
            query = query.Where(a => a.CreatedAt < toDate.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(a => a.Description.ToLower().Contains(keyword.ToLower())
                                   || a.Action.ToLower().Contains(keyword.ToLower()));

        return await query.ToListAsync();
    }

    public async Task AddAsync(AuditLog entry) => await _context.AuditLogs.AddAsync(entry);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}