using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public interface IUserManagementService
{
    Task<List<UserAccountDto>> GetAllAsync();

    /// <summary>
    /// Enforces the creation hierarchy: SystemAdmin may create Owner or Cashier;
    /// Owner may create Cashier only. Checked against the currently logged-in session.
    /// </summary>
    Task<OperationResult> CreateAsync(CreateUserAccountDto dto);

    Task<OperationResult> ResetPasswordAsync(ResetPasswordDto dto);
    Task<OperationResult> DeactivateAsync(int userId);

    /// <summary>Roles the current session is allowed to assign when creating a user.</summary>
    List<string> GetAssignableRoles();
}