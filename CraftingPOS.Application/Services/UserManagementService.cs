using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Entities;
using CraftingPOS.Domain.Enums;
using CraftingPOS.Domain.Interfaces;
using Serilog;

namespace CraftingPOS.Application.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly CurrentUserContext _currentUserContext;

    public UserManagementService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        CurrentUserContext currentUserContext)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _currentUserContext = currentUserContext ?? throw new ArgumentNullException(nameof(currentUserContext));
    }

    public List<string> GetAssignableRoles()
    {
        var currentRole = _currentUserContext.Session?.RoleName;

        return currentRole switch
        {
            RoleNames.SystemAdmin => new List<string> { RoleNames.Owner, RoleNames.Cashier },
            RoleNames.Owner => new List<string> { RoleNames.Cashier },
            _ => new List<string>()
        };
    }

    public async Task<List<UserAccountDto>> GetAllAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users
            .OrderBy(u => u.Role?.Name ?? string.Empty)
            .ThenBy(u => u.Username)
            .Select(MapToDto)
            .ToList();
    }

    public async Task<OperationResult> CreateAsync(CreateUserAccountDto dto)
    {
        if (dto == null)
        {
            return OperationResult.Fail("User payload cannot be null.");
        }

        var assignableRoles = GetAssignableRoles();

        if (string.IsNullOrWhiteSpace(dto.RoleName) || !assignableRoles.Contains(dto.RoleName))
        {
            return OperationResult.Fail("You are not authorized to create an account with that role.");
        }

        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
        {
            return OperationResult.Fail("Username and password are required.");
        }

        if (dto.Password.Length < 6)
        {
            return OperationResult.Fail("Password must be at least 6 characters.");
        }

        var trimmedUsername = dto.Username.Trim();
        var existing = await _userRepository.GetByUsernameAsync(trimmedUsername);
        if (existing != null)
        {
            return OperationResult.Fail($"Username '{trimmedUsername}' is already taken.");
        }

        var role = await _roleRepository.GetByNameAsync(dto.RoleName);
        if (role == null)
        {
            return OperationResult.Fail("Selected role does not exist.");
        }

        var currentUsername = _currentUserContext.Session?.Username ?? "system";

        var user = new User
        {
            RoleId = role.Id,
            Username = trimmedUsername,
            FullName = dto.FullName?.Trim() ?? string.Empty,
            PasswordHash = _passwordHasher.Hash(dto.Password),
            IsActive = true,
            CreatedBy = currentUsername
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        Log.Information("User '{Username}' ({Role}) created by '{Creator}'.", user.Username, dto.RoleName, currentUsername);

        return OperationResult.Ok();
    }

    public async Task<OperationResult> ResetPasswordAsync(ResetPasswordDto dto)
    {
        if (dto == null)
        {
            return OperationResult.Fail("Reset payload cannot be null.");
        }

        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
        {
            return OperationResult.Fail("Password must be at least 6 characters.");
        }

        var user = await _userRepository.GetByIdAsync(dto.UserId);
        if (user == null)
        {
            return OperationResult.Fail("User not found.");
        }

        user.PasswordHash = _passwordHasher.Hash(dto.NewPassword);
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.UpdatedBy = _currentUserContext.Session?.Username ?? "system";

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        Log.Information("Password reset for user '{Username}' by '{Admin}'.", user.Username, _currentUserContext.Session?.Username ?? "system");

        return OperationResult.Ok();
    }

    public async Task<OperationResult> DeactivateAsync(int userId)
    {
        if (_currentUserContext.Session?.UserId == userId)
        {
            return OperationResult.Fail("You cannot deactivate your own account.");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return OperationResult.Fail("User not found.");
        }

        user.IsActive = false;
        user.UpdatedBy = _currentUserContext.Session?.Username ?? "system";

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        Log.Information("User '{Username}' deactivated by '{Admin}'.", user.Username, _currentUserContext.Session?.Username ?? "system");

        return OperationResult.Ok();
    }

    private static UserAccountDto MapToDto(User u)
    {
        return new UserAccountDto
        {
            Id = u.Id,
            Username = u.Username,
            FullName = u.FullName,
            RoleName = u.Role?.Name ?? string.Empty,
            IsActive = u.IsActive,
            LastLoginAt = u.LastLoginAt
        };
    }
}