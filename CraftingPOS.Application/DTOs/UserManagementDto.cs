namespace CraftingPOS.Application.DTOs;

public class UserAccountDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class CreateUserAccountDto
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty; // "Owner" or "Cashier" (never SystemAdmin via UI)
}

public class ResetPasswordDto
{
    public int UserId { get; set; }
    public string NewPassword { get; set; } = string.Empty;
}