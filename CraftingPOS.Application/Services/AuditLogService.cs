using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Interfaces;
using Serilog;

namespace CraftingPOS.Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly CurrentUserContext _currentUserContext;

    public AuditLogService(IAuditLogRepository auditLogRepository, CurrentUserContext currentUserContext)
    {
        _auditLogRepository = auditLogRepository;
        _currentUserContext = currentUserContext;
    }

    public async Task LogAsync(string module, string action, string description)
    {
        try
        {
            var entry = new AuditLog
            {
                Username = _currentUserContext.Session?.Username ?? "system",
                Module = module,
                Action = action,
                Description = description,
                CreatedBy = _currentUserContext.Session?.Username ?? "system"
            };

            await _auditLogRepository.AddAsync(entry);
            await _auditLogRepository.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // An audit-log failure must never break the underlying business action.
            Log.Error(ex, "Failed to write audit log entry: {Module}/{Action} — {Description}", module, action, description);
        }
    }

    public async Task<List<AuditLogDto>> SearchAsync(AuditLogSearchDto criteria)
    {
        var results = await _auditLogRepository.SearchAsync(
            criteria.Module, criteria.Username, criteria.FromDate, criteria.ToDate, criteria.Keyword);

        return results
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new AuditLogDto
            {
                CreatedAt = r.CreatedAt,
                Username = r.Username,
                Module = r.Module,
                Action = r.Action,
                Description = r.Description
            })
            .ToList();
    }
}