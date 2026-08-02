namespace CraftingPOS.Licensing;

public class LicenseData
{
    public string BusinessName { get; set; } = string.Empty;
    public string MachineFingerprint { get; set; } = string.Empty;
    public LicenseType LicenseType { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; } // null for Lifetime

    /// <summary>
    /// Deterministic string used as the signing input, instead of raw JSON
    /// (whose property ordering could theoretically vary across runtimes).
    /// </summary>
    public string ToCanonicalString()
    {
        var expires = ExpiresAt.HasValue ? ExpiresAt.Value.ToString("O") : "None";
        return $"{BusinessName}|{MachineFingerprint}|{LicenseType}|{IssuedAt:O}|{expires}";
    }
}