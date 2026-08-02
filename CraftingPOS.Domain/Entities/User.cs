using CraftingPOS.Domain.Common;

namespace CraftingPOS.Domain.Entities;

public class User : BaseEntity
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime? LastLoginAt { get; set; }

    // Security: failed login tracking / account lockout (SEC requirement)
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEnd { get; set; }

    public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
}