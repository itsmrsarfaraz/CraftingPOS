using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;
using CraftingPOS.Application.Interfaces;
using CraftingPOS.Domain.Interfaces;
using Serilog;

namespace CraftingPOS.Application.Services;

public class AuthService : IAuthService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly CurrentUserContext _currentUserContext;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        CurrentUserContext currentUserContext)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _currentUserContext = currentUserContext;
    }

    public async Task<LoginResultDto> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return LoginResultDto.Fail("Username and password are required.");
        }

        var user = await _userRepository.GetByUsernameAsync(username.Trim());

        if (user == null)
        {
            Log.Warning("Login failed: username '{Username}' does not exist.", username);
            return LoginResultDto.Fail("Invalid username or password.");
        }

        if (!user.IsActive)
        {
            Log.Warning("Login failed: account '{Username}' is disabled.", username);
            return LoginResultDto.Fail("This account has been disabled. Contact the owner.");
        }

        if (user.IsLockedOut)
        {
            var minutesLeft = Math.Ceiling((user.LockoutEnd!.Value - DateTime.UtcNow).TotalMinutes);
            Log.Warning("Login blocked: '{Username}' is locked out for {Minutes} more minute(s).", username, minutesLeft);
            return LoginResultDto.Fail($"Account locked due to too many failed attempts. Try again in {minutesLeft} minute(s).");
        }

        var passwordValid = _passwordHasher.Verify(password, user.PasswordHash);

        if (!passwordValid)
        {
            user.FailedLoginAttempts++;

            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.Add(LockoutDuration);
                Log.Warning("Account '{Username}' locked out after {Attempts} failed attempts.", username, user.FailedLoginAttempts);
            }

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();

            Log.Warning("Login failed: incorrect password for '{Username}'. Attempt {Attempts}/{Max}.",
                username, user.FailedLoginAttempts, MaxFailedAttempts);

            return LoginResultDto.Fail("Invalid username or password.");
        }

        // Successful login: reset lockout state
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user);
        await _userRepository.SaveChangesAsync();

        var session = new UserSessionDto
        {
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            RoleName = user.Role.Name,
            TenantId = user.TenantId
        };

        _currentUserContext.SetSession(session);

        Log.Information("User '{Username}' logged in successfully with role '{Role}'.", user.Username, user.Role.Name);

        return LoginResultDto.Ok(session);
    }

    public void Logout()
    {
        if (_currentUserContext.Session != null)
        {
            Log.Information("User '{Username}' logged out.", _currentUserContext.Session.Username);
        }

        _currentUserContext.Clear();
    }
}