namespace CraftingPOS.Application.DTOs;

public class AuditLogDto
{
    public DateTime CreatedAt { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class AuditLogSearchDto
{
    public string? Module { get; set; }
    public string? Username { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Keyword { get; set; }
}