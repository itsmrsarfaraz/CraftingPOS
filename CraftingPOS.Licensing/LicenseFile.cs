namespace CraftingPOS.Licensing;

public class LicenseFile
{
    public LicenseData Data { get; set; } = new();
    public string SignatureBase64 { get; set; } = string.Empty;
}