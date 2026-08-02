using CraftingPOS.Application.Common;
using CraftingPOS.Application.Services;
using CraftingPOS.Domain.Interfaces;
using CraftingPOS.Infrastructure.Security;
using CraftingPOS.Persistence.Repositories;
using CraftingPOS.Tests.TestSupport;
using Xunit;

namespace CraftingPOS.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly SqliteInMemoryFixture _fixture;
    private readonly IUserRepository _userRepository;
    private readonly BCryptPasswordHasher _hasher = new();
    private readonly AuthService _authService;
    private readonly CurrentUserContext _currentUserContext = new();

    public AuthServiceTests()
    {
        _fixture = new SqliteInMemoryFixture();
        _userRepository = new UserRepository(_fixture.Context);

        var (ownerRole, _) = TestSeed.SeedRoles(_fixture.Context);
        TestSeed.SeedUser(_fixture.Context, ownerRole, "owner1", _hasher.Hash("Correct@123"));

        _authService = new AuthService(_userRepository, _hasher, _currentUserContext, new FakeAuditLogService());
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_Succeeds()
    {
        var result = await _authService.LoginAsync("owner1", "Correct@123");

        Assert.True(result.Success);
        Assert.NotNull(result.Session);
        Assert.Equal("owner1", result.Session!.Username);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Fails()
    {
        var result = await _authService.LoginAsync("owner1", "WrongPassword");

        Assert.False(result.Success);
        Assert.False(_currentUserContext.IsLoggedIn);
    }

    [Fact]
    public async Task Login_WithNonexistentUsername_FailsWithGenericMessage()
    {
        // Security: must not reveal whether the username exists.
        var result = await _authService.LoginAsync("nobody", "whatever");

        Assert.False(result.Success);
        Assert.Equal("Invalid username or password.", result.ErrorMessage);
    }

    [Fact]
    public async Task Login_AfterFiveFailedAttempts_LocksAccount()
    {
        // BR/FR-AUTH: 5 failed attempts -> 15 minute lockout.
        for (var i = 0; i < 5; i++)
        {
            await _authService.LoginAsync("owner1", "WrongPassword");
        }

        var result = await _authService.LoginAsync("owner1", "Correct@123");

        Assert.False(result.Success);
        Assert.Contains("locked", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_ResetsFailedAttempts_OnSuccess()
    {
        await _authService.LoginAsync("owner1", "WrongPassword");
        await _authService.LoginAsync("owner1", "WrongPassword");

        var success = await _authService.LoginAsync("owner1", "Correct@123");
        Assert.True(success.Success);

        // Confirm the counter actually reset in the DB, not just in memory.
        var user = await _userRepository.GetByUsernameAsync("owner1");
        Assert.Equal(0, user!.FailedLoginAttempts);
    }

    public void Dispose() => _fixture.Dispose();
}