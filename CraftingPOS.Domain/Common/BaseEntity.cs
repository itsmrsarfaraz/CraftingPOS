namespace CraftingPOS.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Provides auditing, soft-delete, and multi-tenant readiness (TenantId).
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }

    // Version 1 always uses TenantId = 1.
    // Reserved for future multi-tenant SaaS versions.
    public int TenantId { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}