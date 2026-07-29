namespace CraftingPOS.Application.DTOs;

public class LoginResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public UserSessionDto? Session { get; set; }

    public static LoginResultDto Fail(string message) => new() { Success = false, ErrorMessage = message };
    public static LoginResultDto Ok(UserSessionDto session) => new() { Success = true, Session = session };
}