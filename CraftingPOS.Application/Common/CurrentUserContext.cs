using CraftingPOS.Application.DTOs;

namespace CraftingPOS.Application.Common;

/// <summary>
/// Holds the currently logged-in user's session for the lifetime of the app run.
/// Registered as a Singleton in DI so every ViewModel/Service sees the same session.
/// </summary>
public class CurrentUserContext
{
    public UserSessionDto? Session { get; private set; }

    public bool IsLoggedIn => Session != null;

    public void SetSession(UserSessionDto session) => Session = session;

    public void Clear() => Session = null;
}