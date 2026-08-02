using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace CraftingPOS.Licensing;

/// <summary>
/// FR-LIC-001 / SRS Part 8 §10: generates a stable machine fingerprint from
/// CPU, motherboard, and storage identifiers. Combining all three (rather
/// than any single one) makes the fingerprint resistant to simple component
/// swaps while remaining stable across reboots (BR: "fingerprint must remain
/// stable across reboots").
/// </summary>
public static class MachineFingerprintGenerator
{
    public static string Generate()
    {
        var cpuId = GetWmiProperty("Win32_Processor", "ProcessorId");
        var boardSerial = GetWmiProperty("Win32_BaseBoard", "SerialNumber");
        var diskSerial = GetWmiProperty("Win32_DiskDrive", "SerialNumber");

        var raw = $"{cpuId}|{boardSerial}|{diskSerial}";

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(raw));
        var hex = Convert.ToHexString(hash);

        var shortHex = hex[..16];
        return $"CPOS-{shortHex[..4]}-{shortHex[4..8]}-{shortHex[8..12]}-{shortHex[12..16]}";
    }

    private static string GetWmiProperty(string wmiClass, string property)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
            foreach (ManagementObject obj in searcher.Get())
            {
                var value = obj[property]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    return value.Trim();
            }
        }
        catch
        {
            // WMI can be restricted in some environments (VMs, group policy); degrade gracefully
            // rather than crashing licensing entirely.
        }

        return "UNKNOWN";
    }
}