using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResultDto> LoginAsync(string username, string password);
    void Logout();

    /// <summary>
    /// Verifies credentials belong to an active Owner or SystemAdmin, WITHOUT
    /// changing the current session. Used for in-transaction authorization
    /// (discount override, price-below-cost override) so a cashier doesn't
    /// have to log out to get approval.
    /// </summary>
    Task<bool> VerifyManagerCredentialsAsync(string username, string password);
}