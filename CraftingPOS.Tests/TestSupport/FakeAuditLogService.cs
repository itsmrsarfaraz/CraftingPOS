using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;

namespace CraftingPOS.Tests.TestSupport;

/// <summary>
/// No-op audit logger for tests — audit logging is verified separately
/// (or not at all, for these unit tests) so we don't need a real DB write
/// here; this just satisfies the constructor dependency cleanly.
/// </summary>
public class FakeAuditLogService : IAuditLogService
{
    public List<(string Module, string Action, string Description)> Entries { get; } = new();

    public Task LogAsync(string module, string action, string description)
    {
        Entries.Add((module, action, description));
        return Task.CompletedTask;
    }

    public Task<List<AuditLogDto>> SearchAsync(AuditLogSearchDto criteria)
    {
        return Task.FromResult(new List<AuditLogDto>());
    }
}