namespace CraftingPOS.Licensing;

public class LicenseValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public LicenseData? Data { get; set; }

    public static LicenseValidationResult Ok(LicenseData data) => new() { IsValid = true, Data = data };
    public static LicenseValidationResult Fail(string message) => new() { IsValid = false, ErrorMessage = message };
}