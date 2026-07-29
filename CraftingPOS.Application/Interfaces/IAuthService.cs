using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResultDto> LoginAsync(string username, string password);
    void Logout();
}