using CraftingPOS.Application.Common;
using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Tests.TestSupport;

/// <summary>Builds a CurrentUserContext pre-populated with a session, for tests that need an "already logged in" state.</summary>
public static class FakeCurrentUserContext
{
    public static CurrentUserContext For(int userId, string username, string roleName)
    {
        var context = new CurrentUserContext();
        context.SetSession(new UserSessionDto
        {
            UserId = userId,
            Username = username,
            FullName = username,
            RoleName = roleName,
            TenantId = 1
        });
        return context;
    }
}